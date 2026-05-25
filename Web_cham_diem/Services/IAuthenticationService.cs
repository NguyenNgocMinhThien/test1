using Web_cham_diem.ViewModels;

namespace Web_cham_diem.Services;

public interface IAuthenticationService
{
    Task<bool> RegisterAsync(RegisterViewModel model);
    Task<bool> LoginAsync(LoginViewModel model);
    Task LogoutAsync();
}