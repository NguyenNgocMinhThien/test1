using Web_cham_diem.Models;
using Web_cham_diem.ViewModels;

namespace Web_cham_diem.Services;

public interface IAuthenticationService
{
    Task<(bool Success, string Message)> RegisterAsync(RegisterViewModel model);
    Task<(bool Success, Users? User, string Message)> LoginAsync(LoginViewModel model);
    Task LogoutAsync();
}