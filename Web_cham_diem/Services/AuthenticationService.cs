using Web_cham_diem.Models;
using Web_cham_diem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Web_cham_diem.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;

    public AuthenticationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RegisterAsync(RegisterViewModel model)
    {
        try
        {
            // Kiểm tra email đã tồn tại
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existingUser != null)
                return false;

            // Tạo người dùng mới
            var newUser = new Users
            {
                FullName = model.FullName,
                Email = model.Email,
                StudentId = model.StudentCode,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleId = 2, // Student role
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LoginAsync(LoginViewModel model)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
                return false;

            // Kiểm tra mật khẩu
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (isValidPassword)
            {
                // Cập nhật LastLogin
                user.LastLogin = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await Task.CompletedTask;
    }
}