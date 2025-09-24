using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swd392.Api.Domain.Repositories;
using Swd392.Api.Infrastructure.Database.Entities;
using Swd392.Api.Infrastructure.Database;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Swd392.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly KidsMathDbContext _db;
        private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _env;
    private readonly Swd392.Api.Infrastructure.Email.IEmailSender _emailSender;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _jwtKey;

        public AuthController(IUnitOfWork uow, IConfiguration config, KidsMathDbContext db, IMemoryCache cache, IWebHostEnvironment env, Swd392.Api.Infrastructure.Email.IEmailSender emailSender, IServiceScopeFactory scopeFactory)
        {
            _uow = uow;
            _config = config;
            _db = db;
            _cache = cache;
            _env = env;
            _emailSender = emailSender;
            _scopeFactory = scopeFactory;
            _jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key configuration");
        }

        private static bool IsEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new MailAddress(email.Trim());
                // Chỉ cần có dạng hợp lệ chứa '@'
                return addr.Address.Contains('@');
            }
            catch { return false; }
        }

    public record LoginRequest(string username, string password);
    public record AccountResponse(Guid account_id, string username, string? email, string? role, DateTime? created_at, DateTime? updated_at);
    

        public record RequestParentRegistration(
            string username,
            string email,
            string password,
            string? full_name,
            string? phone
        );
        public record ConfirmParentRegistration(string email, string code);

        private class PendingParentRegistration
        {
            public required string Username { get; init; }
            public required string Email { get; init; }
            public required string PasswordHash { get; init; }
            public string? FullName { get; init; }
            public string? Phone { get; init; }
            public required string Code { get; init; }
            public required Guid AccountId { get; init; }
            public DateTime ExpiresAtUtc { get; init; }
        }

    
  
    [HttpPost("register/parent/request")]
    [AllowAnonymous]
        public async Task<IActionResult> RequestParentRegistrationAsync([FromBody] RequestParentRegistration req)
        {
            if (string.IsNullOrWhiteSpace(req.username) || string.IsNullOrWhiteSpace(req.email) || string.IsNullOrWhiteSpace(req.password))
                return BadRequest("username, email, password are required");
            if (!IsEmail(req.email))
                return BadRequest("Invalid email format");
            if (req.password.Length < 6)
                return BadRequest("Password must be at least 6 characters");

          
            if (await _uow.Accounts.GetByUsernameAsync(req.username) is not null)
                return Conflict("Username already exists");
            if (await _uow.Accounts.GetByEmailAsync(req.email) is not null)
                return Conflict("Email already exists");

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var now = DateTime.UtcNow;
            var pendingAccount = new Account
            {
                username = req.username,
                email = req.email,
                password_hash = BCrypt.Net.BCrypt.HashPassword(req.password),
                role = "parent",
                fullname = req.full_name ?? req.username, // ensure NOT NULL fullname
                created_at = now,
                updated_at = now
            };
            await _uow.Accounts.AddAsync(pendingAccount);
            await _uow.SaveAsync();

            var pending = new PendingParentRegistration
            {
                Username = req.username,
                Email = req.email,
                PasswordHash = pendingAccount.password_hash,
                FullName = req.full_name,
                Phone = req.phone,
                Code = code,
                AccountId = pendingAccount.account_id,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2)
            };

            var pendingKey = $"pending:parent:{req.email.ToLowerInvariant()}";
            _cache.Set(pendingKey, pending, TimeSpan.FromMinutes(2));

            // Cleanup of stale unconfirmed accounts now handled by PendingAccountCleanupService

            // Always send via email now
            try
            {
                var subject = "KidsMath - Verify your email (Parent)";
                var body = $"<p>Your verification code is:</p><h2>{code}</h2><p>This code expires in 2 minutes.</p>";
                await _emailSender.SendAsync(req.email, subject, body);
            }
            catch (Exception ex)
            {
                // rollback account if email fails
                try
                {
                    var acc = await _uow.Accounts.GetByIdAsync(pendingAccount.account_id);
                    if (acc != null)
                    {
                        _db.Accounts.Remove(acc);
                        await _db.SaveChangesAsync();
                    }
                }
                catch { }
                return StatusCode(502, new { error = "Failed to send verification code", detail = ex.Message });
            }

            var devCode = _env.IsDevelopment() ? code : null;
            return Accepted(new { message = "Verification code sent. Confirm within 2 minutes.", email = req.email, dev_code = devCode });
        }

    // Parent email confirmation (final version)
    [HttpPost("register/parent/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmParentRegistrationAsync([FromBody] ConfirmParentRegistration req)
    {
        Console.WriteLine("[CONFIRM-PARENT] Incoming request");
        if (string.IsNullOrWhiteSpace(req.email) || string.IsNullOrWhiteSpace(req.code))
            return BadRequest("email and code are required");
        if (!IsEmail(req.email))
            return BadRequest("Invalid email format");

        var pendingKey = $"pending:parent:{req.email.ToLowerInvariant()}";
        if (!_cache.TryGetValue<PendingParentRegistration>(pendingKey, out var pending) || pending is null)
            return BadRequest("No pending registration or it has expired");
        if (!string.Equals(pending.Code, req.code, StringComparison.Ordinal))
            return BadRequest("Invalid verification code");
        if (DateTime.UtcNow > pending.ExpiresAtUtc)
            return BadRequest("Verification code expired");

        var parentAccount = await _uow.Accounts.GetByIdAsync(pending.AccountId);
        if (parentAccount is null)
            return BadRequest("Pending account no longer exists");
        parentAccount.updated_at = DateTime.UtcNow;
        await _uow.SaveAsync();

        // Idempotency: only add parent profile if not exists
        var existingParent = await _db.Parents.FindAsync(parentAccount.account_id);
        if (existingParent == null)
        {
            _db.Parents.Add(new Parent
            {
                parent_id = parentAccount.account_id,
                username = parentAccount.username,
                email = parentAccount.email,
                password_hash = parentAccount.password_hash,
                full_name = pending.FullName ?? parentAccount.username,
                phone = pending.Phone,
                role = parentAccount.role ?? "parent",
                created_at = parentAccount.created_at,
                updated_at = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        _cache.Remove(pendingKey);
        var resp = new AccountResponse(
            parentAccount.account_id,
            parentAccount.username,
            parentAccount.email,
            parentAccount.role,
            parentAccount.created_at,
            parentAccount.updated_at
        );
        Console.WriteLine($"[CONFIRM-PARENT] Done account_id={parentAccount.account_id}");
        return new ObjectResult(resp) { StatusCode = StatusCodes.Status200OK };
    }

        public record ForgotPasswordRequest(string email);
        public record ForgotPasswordResponse(string email, string message, string? dev_code, string? dev_token);
        public record ResetPasswordRequest(string email, string new_password, string? code, string? token);

    
    [HttpPost("password/forgot")]
    [AllowAnonymous]
        public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.email)) return BadRequest("email is required");
            if (!IsEmail(req.email)) return BadRequest("Invalid email format");

            var account = await _uow.Accounts.GetByEmailAsync(req.email);

            string? devCode = null;
            string? devToken = null;
            if (account is not null)
            {
                var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); 

                _cache.Set($"reset:code:{req.email.ToLowerInvariant()}", code, TimeSpan.FromMinutes(10));
                _cache.Set($"reset:token:{token}", req.email.ToLowerInvariant(), TimeSpan.FromMinutes(15));

                var resetBaseUrl = _config["App:PasswordResetUrl"]; 
                string resetLink;
                if (!string.IsNullOrWhiteSpace(resetBaseUrl))
                {
                    resetLink = $"{resetBaseUrl}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(req.email)}";
                }
                else
                {
                    // Fallback: point to Swagger with query for convenience (front-end should handle real link)
                    resetLink = $"https://localhost:7120/swagger/index.html?resetToken={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(req.email)}";
                }

                try
                {
                    var subject = "KidsMath - Password reset";
                    var body = $@"<p>You requested to reset your password.</p>
<p>Your OTP code:</p><h2>{code}</h2>
<p>This code expires in 10 minutes.</p>
<p>Or click the link below to reset your password:</p>
<p><a href=""{resetLink}"">Reset Password</a> (link expires in ~15 minutes)</p>";
                    await _emailSender.SendAsync(req.email, subject, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Email] Failed to send password reset to {req.email}: {ex.Message}");
                }

                if (_env.IsDevelopment())
                {
                    devCode = code;
                    devToken = token;
                }
            }

            return Accepted(new ForgotPasswordResponse(req.email, "If that account exists, we've sent an email with instructions.", devCode, devToken));
        }

    [HttpPost("password/reset")]
    [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.email) || string.IsNullOrWhiteSpace(req.new_password))
                return BadRequest("email and new_password are required");
            if (!IsEmail(req.email))
                return BadRequest("Invalid email format");
            if (req.new_password.Length < 6)
                return BadRequest("Password must be at least 6 characters");

            var emailKey = req.email.ToLowerInvariant();
            var byCode = !string.IsNullOrWhiteSpace(req.code);
            var byToken = !string.IsNullOrWhiteSpace(req.token);
            if (!byCode && !byToken)
                return BadRequest("Either code or token is required");

            bool valid = false;
            if (byCode)
            {
                if (_cache.TryGetValue<string>($"reset:code:{emailKey}", out var expected) && expected == req.code)
                    valid = true;
            }
            else if (byToken)
            {
                if (_cache.TryGetValue<string>($"reset:token:{req.token}", out var tokenEmail) && string.Equals(tokenEmail, emailKey, StringComparison.OrdinalIgnoreCase))
                    valid = true;
            }

            if (!valid)
                return BadRequest("Invalid or expired code/token");

            var account = await _uow.Accounts.GetByEmailAsync(req.email);
            if (account is null)
                return BadRequest("Account not found");

            account.password_hash = BCrypt.Net.BCrypt.HashPassword(req.new_password);
            account.updated_at = DateTime.UtcNow;
            await _uow.SaveAsync();

            _cache.Remove($"reset:code:{emailKey}");
            if (byToken)
                _cache.Remove($"reset:token:{req.token}");

            return Ok(new { message = "Password has been reset." });
        }

        private class PendingStudentRegistration
        {
            public required string Username { get; init; }
            public required string Email { get; init; }
            public required string PasswordHash { get; init; }
            public string? FullName { get; init; }
            public required Guid ParentAccountId { get; init; }
            public required string Code { get; init; }
            public required Guid AccountId { get; init; }
            public DateTime ExpiresAtUtc { get; init; }
            public DateOnly? Birthday { get; init; }
            public int? Grade { get; init; }
        }

        public record RequestStudentRegistration(
            string username,
            string email,
            string password,
            string? full_name,
            DateOnly? birthday,
            int? grade
        );
        public record ConfirmStudentRegistration(string email, string code);
        public record StudentProfileResponse(Guid student_id, string username, string full_name, DateOnly? birthday, int? grade, Guid? parent_id, string role, DateTime created_at, DateTime updated_at);
        public record UpdateStudentProfileRequest(string? full_name, DateOnly? birthday, int? grade);

        [HttpPost("register/student/request")]
        [Authorize(Roles = "admin,parent")]
    public async Task<IActionResult> RequestStudentRegistrationAsync([FromBody] RequestStudentRegistration req)
        {
            if (string.IsNullOrWhiteSpace(req.username) || string.IsNullOrWhiteSpace(req.email) || string.IsNullOrWhiteSpace(req.password))
                return BadRequest("username, email, password are required");

            if (!IsEmail(req.email))
                return BadRequest("Invalid email format");
            if (req.password.Length < 6) return BadRequest("Password must be at least 6 characters");

            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(sub, out var parentAccountId)) return Unauthorized("Invalid token subject");
            var parentAccount = await _uow.Accounts.GetByIdAsync(parentAccountId);
            if (parentAccount is null || !string.Equals(parentAccount.role, "parent", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (await _uow.Accounts.GetByUsernameAsync(req.username) is not null)
                return Conflict("Username already exists");
            if (await _uow.Accounts.GetByEmailAsync(req.email) is not null)
                return Conflict("Email already exists");

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var now = DateTime.UtcNow;
            var pendingAccount = new Account
            {
                username = req.username,
                email = req.email,
                password_hash = BCrypt.Net.BCrypt.HashPassword(req.password),
                role = "student",
                fullname = req.full_name ?? req.username, // ensure NOT NULL fullname
                created_at = now,
                updated_at = now
            };
            await _uow.Accounts.AddAsync(pendingAccount);
            await _uow.SaveAsync();

            var pending = new PendingStudentRegistration
            {
                Username = req.username,
                Email = req.email,
                PasswordHash = pendingAccount.password_hash,
                FullName = req.full_name,
                ParentAccountId = parentAccountId,
                Code = code,
                AccountId = pendingAccount.account_id,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
                Birthday = req.birthday,
                Grade = req.grade
            };

            var pendingKey = $"pending:student:{req.email.ToLowerInvariant()}";
            _cache.Set(pendingKey, pending, TimeSpan.FromMinutes(2));

            // Cleanup handled centrally by PendingAccountCleanupService

            // Always send via email now
            try
            {
                var subject = "KidsMath - Verify your email (Student)";
                var body = $"<p>Your verification code is:</p><h2>{code}</h2><p>This code expires in 2 minutes.</p>";
                await _emailSender.SendAsync(req.email, subject, body);
            }
            catch (Exception ex)
            {
                // rollback account
                try
                {
                    var acc = await _uow.Accounts.GetByIdAsync(pendingAccount.account_id);
                    if (acc != null)
                    {
                        _db.Accounts.Remove(acc);
                        await _db.SaveChangesAsync();
                    }
                }
                catch { }
                return StatusCode(502, new { error = "Failed to send verification code", detail = ex.Message });
            }

            var devCode = _env.IsDevelopment() ? code : null;
            return Accepted(new { message = "Verification code sent. Confirm within 2 minutes.", email = req.email, dev_code = devCode });
        }

    [HttpGet("student/me")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> GetStudentProfileAsync()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var studentId)) return Unauthorized();
        var student = await _db.Students.FindAsync(studentId);
        if (student is null) return NotFound("Student profile not found");
        var resp = new StudentProfileResponse(
            student.student_id,
            student.username,
            student.full_name,
            student.birthday,
            student.grade,
            student.parent_id,
            student.role,
            student.created_at,
            student.updated_at
        );
        return Ok(resp);
    }

    [HttpPut("student/me")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> UpdateStudentProfileAsync([FromBody] UpdateStudentProfileRequest req)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var studentId)) return Unauthorized();
        var student = await _db.Students.FindAsync(studentId);
        if (student is null) return NotFound("Student profile not found");

        bool changed = false;
        if (!string.IsNullOrWhiteSpace(req.full_name) && req.full_name != student.full_name)
        {
            student.full_name = req.full_name.Trim();
            changed = true;
        }
        if (req.birthday.HasValue && req.birthday != student.birthday)
        {
            student.birthday = req.birthday;
            changed = true;
        }
        if (req.grade.HasValue && req.grade != student.grade)
        {
            if (req.grade < 0) return BadRequest("grade must be >= 0");
            student.grade = req.grade;
            changed = true;
        }
        if (changed)
        {
            student.updated_at = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        var resp = new StudentProfileResponse(
            student.student_id,
            student.username,
            student.full_name,
            student.birthday,
            student.grade,
            student.parent_id,
            student.role,
            student.created_at,
            student.updated_at
        );
        return Ok(resp);
    }

    [HttpPost("register/student/confirm")]
    [Authorize(Roles = "admin,parent")]
        public async Task<IActionResult> ConfirmStudentRegistrationAsync([FromBody] ConfirmStudentRegistration req)
        {
            if (string.IsNullOrWhiteSpace(req.email) || string.IsNullOrWhiteSpace(req.code))
                return BadRequest("email and code are required");
            if (!IsEmail(req.email))
                return BadRequest("Invalid email format");

            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(sub, out var parentAccountId)) return Unauthorized("Invalid token subject");

            var pendingKey = $"pending:student:{req.email.ToLowerInvariant()}";
            if (!_cache.TryGetValue<PendingStudentRegistration>(pendingKey, out var pending) || pending is null)
                return BadRequest("No pending registration or it has expired");

            if (pending.ParentAccountId != parentAccountId)
                return Forbid();
            if (!string.Equals(pending.Code, req.code, StringComparison.Ordinal))
                return BadRequest("Invalid verification code");
            if (DateTime.UtcNow > pending.ExpiresAtUtc)
                return BadRequest("Verification code expired");

            var studentAccount = await _uow.Accounts.GetByIdAsync(pending.AccountId);
            if (studentAccount is null)
                return BadRequest("Pending account no longer exists");
            studentAccount.updated_at = DateTime.UtcNow;
            await _uow.SaveAsync();

            _db.Students.Add(new Student
            {
                student_id = studentAccount.account_id,
                username = pending.Username,
                password_hash = pending.PasswordHash,
                full_name = pending.FullName ?? pending.Username,
                parent_id = pending.ParentAccountId,
                role = "student",
                birthday = pending.Birthday,
                grade = pending.Grade,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
  
            var parentProfile = await _db.Parents.FindAsync(pending.ParentAccountId);
            if (parentProfile is null)
            {
                var parentAcc = await _uow.Accounts.GetByIdAsync(pending.ParentAccountId);
                _db.Parents.Add(new Parent
                {
                    parent_id = pending.ParentAccountId,
                    username = parentAcc?.username ?? "parent", // fallback
                    email = parentAcc?.email ?? (parentAcc?.username + "@placeholder"),
                    password_hash = parentAcc?.password_hash ?? string.Empty,
                    full_name = parentAcc?.username ?? "Parent",
                    phone = null,
                    role = parentAcc?.role ?? "parent",
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            // existingStudent already created above with parent_id set

            _cache.Remove(pendingKey);

            var resp = new AccountResponse(
                studentAccount.account_id,
                studentAccount.username,
                studentAccount.email,
                studentAccount.role,
                studentAccount.created_at,
                studentAccount.updated_at
            );
            // Simplified: avoid CreatedAtAction (no specific GET route for this resource)
            return Ok(resp);
        }


    #region Authentication

    [HttpPost("login")]
    [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var identifier = req.username;
            var account = await _uow.Accounts.GetByUsernameAsync(identifier);
            if (account == null && identifier.Contains('@'))
            {
                account = await _uow.Accounts.GetByEmailAsync(identifier);
            }

            if (account == null)
                return Unauthorized("Invalid username or password");

            bool passwordOk;
            var hash = account.password_hash ?? string.Empty;
            if (hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$"))
            {
                
                passwordOk = BCrypt.Net.BCrypt.Verify(req.password, hash);
            }
            else
            {
                passwordOk = string.Equals(req.password, hash);
            }

            if (!passwordOk)
                return Unauthorized("Invalid username or password");

            // Status field removed in new schema; if you reintroduce it, restore validation here.

           
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.account_id.ToString()),
                new Claim("name", account.username),
                new Claim("role", account.role ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
        #endregion
    }
}
