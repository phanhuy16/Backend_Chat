using Core.DTOs.Auth;

namespace Core.Interfaces.IServices
{
    public interface IAuthenticationService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> LogoutAsync(int userId);
        Task<UserAuthDto?> GetCurrentUserAsync(int userId);
        Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request);
    }
}
