using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swd392.Api.Domain.Repositories;
using Swd392.Api.Infrastructure.Database;
using Swd392.Api.Infrastructure.Database.Entities;

namespace Swd392.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly KidsMathDbContext _db;

    public ProfileController(IUnitOfWork uow, KidsMathDbContext db)
    {
        _uow = uow;
        _db = db;
    }

    public record AccountDto(Guid account_id, string username, string? email, string? role, DateTime? created_at, DateTime? updated_at);
    public record ParentDto(Guid parent_id, string? full_name, string? phone);
    public record StudentDto(Guid student_id, string? full_name, string? username, Guid? parent_id);
    public record ChildDto(Guid student_id, string? full_name, string? username);
    public record ParentProfileResponse(AccountDto account, ParentDto parent, IEnumerable<ChildDto> students);
    public record StudentProfileResponse(AccountDto account, StudentDto student, ParentDto? parent);

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        var account = await _uow.Accounts.GetWithRolesByUsernameAsync(username);
        if (account == null) return NotFound();
        var accountDto = new AccountDto(account.account_id, account.username, account.email, account.role, account.created_at, account.updated_at);

        // Student perspective (role == student): fetch student row by same id
        if (string.Equals(account.role, "student", StringComparison.OrdinalIgnoreCase))
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.student_id == account.account_id);
            if (student == null) return Ok(new { account = accountDto, message = "Student profile not found" });

            ParentDto? parentDto = null;
            if (student.parent_id.HasValue)
            {
                var parent = await _db.Parents.FirstOrDefaultAsync(p => p.parent_id == student.parent_id.Value);
                if (parent != null)
                {
                    parentDto = new ParentDto(parent.parent_id, parent.full_name, parent.phone);
                }
            }
            var stdDto = new StudentDto(student.student_id, student.full_name, student.username, student.parent_id);
            return Ok(new StudentProfileResponse(accountDto, stdDto, parentDto));
        }

        // Parent perspective
        if (string.Equals(account.role, "parent", StringComparison.OrdinalIgnoreCase))
        {
            var parent = await _db.Parents.FirstOrDefaultAsync(p => p.parent_id == account.account_id);
            if (parent == null) return Ok(new { account = accountDto, message = "Parent profile not found" });
            var students = await _db.Students.Where(s => s.parent_id == parent.parent_id)
                .Select(s => new ChildDto(s.student_id, s.full_name, s.username))
                .ToListAsync();
            return Ok(new ParentProfileResponse(accountDto, new ParentDto(parent.parent_id, parent.full_name, parent.phone), students));
        }

        // Default fallback (admin/teacher etc.)
        return Ok(new { account = accountDto });
    }
}
