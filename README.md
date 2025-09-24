# KidsMath / Swd392 API (ASP.NET Core .NET 8)

Hệ thống Web API quản lý tài khoản (Parent / Student) với đăng ký qua email OTP, xác thực JWT, Repository + UnitOf Work và cơ chế tự động dọn các tài khoản chưa xác minh.

## Tính năng chính
* Đăng ký Parent / Student với OTP email (thời hạn 2 phút).
* Xác nhận qua mã OTP (6 số) lưu tạm trong memory cache.
* JWT Login (dùng username hoặc email + password).
* Quên mật khẩu: gửi email chứa OTP + token reset.
* Tự động xóa Account chưa xác nhận sau TTL (background service).
* Cấu hình linh hoạt thời gian quét và TTL qua `PendingCleanup`.

## Cấu trúc thư mục chính
```
Swd392.Api/
  Controllers/
    AuthController.cs        # Đăng ký, xác nhận, login, reset password
    ProfileController.cs     # (Ví dụ: thông tin tài khoản / profile)
  Infrastructure/
    Database/                # DbContext + Entities scaffolded
    Repositories/            # Repo + UnitOfWork
    Email/                   # Gửi SMTP
    Services/
      PendingAccountCleanupService.cs
```

## Cấu hình (appsettings.json)
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=KidsMathdata;User Id=sa;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Issuer": "Swd392.Api",
    "Audience": "Swd392.Client",
    "Key": "CHANGE_ME_SECRET_KEY",
    "ExpiresMinutes": 60
  },
  "PendingCleanup": {
    "IntervalSeconds": 60,
    "TtlSeconds": 120
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your@gmail.com",
    "Password": "app_password",
    "From": "KidsMath <no-reply@kidsmath.local>",
    "UseSsl": false,
    "UseStartTls": true
  }
}
```
Khuyên dùng `dotnet user-secrets` hoặc biến môi trường để tránh commit mật khẩu SMTP & JWT key:
```
export ConnectionStrings__Default=...
export Jwt__Key=...
export Smtp__Password=...
```

## Chạy dự án
```bash
cd Swd392.Api
dotnet restore
dotnet build
dotnet run
```
Swagger: https://localhost:7120/swagger

## Luồng đăng ký & xác nhận
1. Parent gọi: `POST /api/auth/register/parent/request`
   ```json
   {
     "username": "parent01",
     "email": "parent01@example.com",
     "password": "abc123",
     "full_name": "Parent 01"
   }
   ```
   Trả về HTTP 202 + dev_code (Development). OTP hiệu lực 2 phút.

2. Parent xác nhận: `POST /api/auth/register/parent/confirm`
   ```json
   { "email": "parent01@example.com", "code": "123456" }
   ```

3. Đăng nhập: `POST /api/auth/login`
   ```json
   { "username": "parent01", "password": "abc123" }
   ```

4. Tạo Student (cần Bearer token của parent): `POST /api/auth/register/student/request`
   ```json
   {
     "username": "student01",
     "email": "student01@example.com",
     "password": "abc123",
     "full_name": "Student 01"
   }
   ```
5. Xác nhận Student: `POST /api/auth/register/student/confirm`

## Quên mật khẩu
`POST /api/auth/password/forgot` → gửi OTP + token
`POST /api/auth/password/reset` với (email + new_password + code) hoặc (email + new_password + token)

## Cơ chế dọn dẹp (PendingAccountCleanupService)
Chạy mỗi `IntervalSeconds` (mặc định 60). Xóa các account:
* role = parent mà chưa có row trong `Parents` và `created_at < now - TTL`.
* role = student mà chưa có row trong `Students` và quá TTL.

TTL được cấu hình qua `PendingCleanup:TtlSeconds` (mặc định 120s).

## Mở rộng tương lai (gợi ý)
* Thêm bảng PendingRegistration thay vì tạo Account trước.
* Rate limiting OTP / xác nhận.
* Bcrypt work factor cấu hình (cost).
* Thêm refresh token flow.
* Logging + Serilog + central structured logging.

## Build & Coding style
* .NET 8, Nullable enabled.
* Repository + UnitOfWork chuẩn cho account handling.
* OTP lưu trong IMemoryCache (có thể chuyển Redis khi scale).

## License
Internal / học tập.

---
Mọi thắc mắc hoặc cần bổ sung endpoint khác cứ tạo issue hoặc hỏi trực tiếp.
