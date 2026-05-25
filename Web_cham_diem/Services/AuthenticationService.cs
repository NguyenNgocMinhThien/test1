using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.ViewModels;

namespace Web_cham_diem.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(ApplicationDbContext context, ILogger<AuthenticationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterViewModel model)
    {
        try
        {
            if (model == null)
                return (false, "Dữ liệu không hợp lệ.");

            var emailLower = model.Email.ToLower().Trim();
            var studentCodeUpper = model.StudentCode.ToUpper().Trim();

            // Kiểm tra email đã tồn tại
            var existingEmail = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == emailLower);

            if (existingEmail)
            {
                _logger.LogWarning($"Đăng ký thất bại: Email {model.Email} đã tồn tại.");
                return (false, "Email này đã được đăng ký trong hệ thống.");
            }

            // Kiểm tra mã sinh viên đã tồn tại
            var existingStudentCode = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.StudentId != null && u.StudentId.ToUpper() == studentCodeUpper);

            if (existingStudentCode)
            {
                _logger.LogWarning($"Đăng ký thất bại: Mã sinh viên {model.StudentCode} đã tồn tại.");
                return (false, "Mã sinh viên này đã được đăng ký.");
            }

            // Tạo người dùng mới
            var newUser = new Users
            {
                FullName = model.FullName.Trim(),
                Email = emailLower,
                StudentId = studentCodeUpper,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                PhoneNumber = string.Empty,
                RoleId = 2, // Student role
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Người dùng mới đăng ký thành công: {emailLower}");
            return (true, "Đăng ký tài khoản thành công! Vui lòng đăng nhập.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Lỗi cơ sở dữ liệu khi đăng ký.");
            return (false, "Lỗi khi lưu dữ liệu. Vui lòng thử lại sau.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định trong quá trình đăng ký.");
            return (false, "Lỗi không xác định. Vui lòng thử lại sau.");
        }
    }

    public async Task<(bool Success, Users? User, string Message)> LoginAsync(LoginViewModel model)
    {
        try
        {
            if (model == null)
                return (false, null, "Dữ liệu không hợp lệ.");

            var emailLower = model.Email.ToLower().Trim();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);

            if (user == null)
            {
                _logger.LogWarning($"Đăng nhập thất bại: Email {model.Email} không tồn tại.");
                return (false, null, "Email không tồn tại hoặc mật khẩu sai.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning($"Đăng nhập thất bại: Tài khoản {emailLower} đã bị khóa.");
                return (false, null, "Tài khoản này đã bị khóa. Liên hệ quản trị viên để biết thêm chi tiết.");
            }

            bool isValidPassword = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!isValidPassword)
            {
                _logger.LogWarning($"Đăng nhập thất bại: Mật khẩu sai cho {emailLower}.");
                return (false, null, "Email không tồn tại hoặc mật khẩu sai.");
            }

            user.LastLogin = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Người dùng {emailLower} đăng nhập thành công.");
            return (true, user, "Đăng nhập thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định trong quá trình đăng nhập.");
            return (false, null, "Lỗi không xác định. Vui lòng thử lại sau.");
        }
    }

    public async Task LogoutAsync()
    {
        await Task.CompletedTask;
    }
}