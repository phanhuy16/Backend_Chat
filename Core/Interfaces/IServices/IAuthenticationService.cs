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
        Task<AuthResponse> LoginWithFacebookAsync(FacebookLoginRequest request);
        Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<EnableTwoFactorResponse?> EnableTwoFactorAsync(int userId);
        Task<AuthResponse> VerifyTwoFactorSetupAsync(int userId, VerifyTwoFactorRequest request);
        Task<bool> DisableTwoFactorAsync(int userId);
        Task<AuthResponse> VerifyTwoFactorLoginAsync(TwoFactorLoginRequest request);
    }
}
