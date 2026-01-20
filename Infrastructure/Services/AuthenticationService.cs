using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.DTOs.Auth;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Google.Apis.Auth;

namespace Infrastructure.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IEmailService _emailService;
        private readonly UserManager<User> _userManager;

        public AuthenticationService(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger,
            IEmailService emailService,
            UserManager<User> userManager)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
            _userManager = userManager;
        }

        public async Task<UserAuthDto?> GetCurrentUserAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                return user != null ? await MapToUserAuthDtoAsync(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin user ID {UserId}", userId);
                return null;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Login failed: thiếu username hoặc password");
                    return new AuthResponse { Success = false, Message = "Username and Password are required" };
                }

                var user = await _userRepository.GetByUsernameAsync(request.Username);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: sai username {Username}", request.Username);
                    return new AuthResponse { Success = false, Message = "Invalid username or password" };
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed: sai password cho user {Username}", request.Username);
                    return new AuthResponse { Success = false, Message = "Invalid username or password" };
                }

                if (await _userManager.GetTwoFactorEnabledAsync(user))
                {
                    return new AuthResponse
                    {
                        Success = true,
                        Message = "2FA required",
                        RequiresTwoFactor = true
                    };
                }

                // Generate tokens
                var token = await GenerateJwtTokenAsync(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.Status = StatusUser.Online;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {Username} đăng nhập thành công", user.UserName);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    User = await MapToUserAuthDtoAsync(user),
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = DateTime.UtcNow.AddMinutes(30)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi login user {Username}", request.Username);
                return new AuthResponse { Success = false, Message = "Error during login" };
            }
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.RefreshToken = string.Empty;
                    user.RefreshTokenExpiryTime = DateTime.MinValue;
                    user.Status = StatusUser.Offline;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _userRepository.UpdateAsync(user);

                    _logger.LogInformation("User ID {UserId} đã logout", userId);
                    return true;
                }

                _logger.LogWarning("Logout thất bại: không tìm thấy user {UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi logout user {UserId}", userId);
                return false;
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(request.Token);
                if (principal == null)
                {
                    return new AuthResponse { Success = false, Message = "Invalid token" };
                }

                var usernameClaim = principal.FindFirst(ClaimTypes.Name);
                if (usernameClaim == null)
                {
                    return new AuthResponse { Success = false, Message = "Invalid token claims" };
                }

                var user = await _userRepository.GetByUsernameAsync(usernameClaim.Value);

                if (user == null ||
                    user.RefreshToken != request.RefreshToken ||
                    user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Token refresh failed for user {Username}", usernameClaim.Value);
                    return new AuthResponse { Success = false, Message = "Invalid refresh token" };
                }

                var newToken = await GenerateJwtTokenAsync(user);
                var newRefreshToken = GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {Username} refresh token thành công", usernameClaim.Value);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Token = newToken,
                    User = await MapToUserAuthDtoAsync(user),
                    RefreshToken = newRefreshToken,
                    ExpiresIn = DateTime.UtcNow.AddMinutes(30)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi refresh token");
                return new AuthResponse { Success = false, Message = "Error refreshing token" };
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return new AuthResponse { Success = false, Message = "Username, Email and Password are required" };
                }

                if (request.Password != request.ConfirmPassword)
                {
                    return new AuthResponse { Success = false, Message = "Passwords do not match" };
                }

                if (request.Password.Length < 6)
                {
                    return new AuthResponse { Success = false, Message = "Password must be at least 6 characters" };
                }

                if (await _userRepository.GetByUsernameAsync(request.Username) != null)
                {
                    return new AuthResponse { Success = false, Message = "Username already exists" };
                }

                if (await _userRepository.GetByEmailAsync(request.Email) != null)
                {
                    return new AuthResponse { Success = false, Message = "Email already exists" };
                }

                var user = new User
                {
                    UserName = request.Username,
                    Email = request.Email,
                    DisplayName = request.DisplayName ?? request.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Avatar = request.Avatar ?? "https://via.placeholder.com/150",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                await _userRepository.AddAsync(user);

                var token = await GenerateJwtTokenAsync(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {Username} đăng ký thành công", user.UserName);

                return new AuthResponse
                {
                    Success = true,
                    Message = "User registered successfully",
                    User = await MapToUserAuthDtoAsync(user),
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = DateTime.UtcNow.AddMinutes(30)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký user {Username}", request.Username);
                return new AuthResponse { Success = false, Message = "Error during registration" };
            }
        }

        public async Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    // Điền Client ID của bạn từ Google Console vào appsettings.json
                    Audience = new List<string> { _configuration["Authentication:Google:ClientId"]! }
                };

                // 1. Xác thực ID Token gửi từ Frontend
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                // 2. Kiểm tra xem user đã tồn tại chưa (dựa trên Email)
                var user = await _userRepository.GetByEmailAsync(payload.Email);

                if (user == null)
                {
                    // 3. Nếu chưa có, tạo user mới
                    user = new User
                    {
                        UserName = payload.Email, // Hoặc logic tạo username riêng
                        Email = payload.Email,
                        DisplayName = payload.Name,
                        Avatar = payload.Picture,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = StatusUser.Online,
                        // Pass hash có thể để trống hoặc chuỗi ngẫu nhiên vì login qua Google
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                    };
                    await _userRepository.AddAsync(user);
                }

                // 4. Tạo JWT token giống như hàm Login thông thường
                var token = await GenerateJwtTokenAsync(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.Status = StatusUser.Online;

                await _userRepository.UpdateAsync(user);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Google login successful",
                    User = await MapToUserAuthDtoAsync(user),
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = DateTime.UtcNow.AddMinutes(30)
                };
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogError(ex, "Google token validation failed");
                return new AuthResponse { Success = false, Message = "Invalid Google Token" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return new AuthResponse { Success = false, Message = "Internal server error during Google login" };
            }
        }

        public async Task<AuthResponse> LoginWithFacebookAsync(FacebookLoginRequest request)
        {
            try
            {
                // 1. Validate Facebook access token by calling Facebook Graph API
                using var httpClient = new HttpClient();
                var appId = _configuration["Authentication:Facebook:AppId"];
                var appSecret = _configuration["Authentication:Facebook:AppSecret"];

                // Verify token with Facebook
                var verifyUrl = $"https://graph.facebook.com/debug_token?input_token={request.AccessToken}&access_token={appId}|{appSecret}";
                var verifyResponse = await httpClient.GetAsync(verifyUrl);

                if (!verifyResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Facebook token verification failed");
                    return new AuthResponse { Success = false, Message = "Invalid Facebook Token" };
                }

                var verifyJson = await verifyResponse.Content.ReadAsStringAsync();
                var verifyData = System.Text.Json.JsonDocument.Parse(verifyJson);
                
                if (!verifyData.RootElement.GetProperty("data").GetProperty("is_valid").GetBoolean())
                {
                    _logger.LogError("Facebook token is not valid");
                    return new AuthResponse { Success = false, Message = "Invalid Facebook Token" };
                }

                var facebookUserId = verifyData.RootElement.GetProperty("data").GetProperty("user_id").GetString();

                // 2. Get user info from Facebook
                var userInfoUrl = $"https://graph.facebook.com/me?fields=id,name,email,picture&access_token={request.AccessToken}";
                var userInfoResponse = await httpClient.GetAsync(userInfoUrl);

                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Facebook user info retrieval failed");
                    return new AuthResponse { Success = false, Message = "Failed to get Facebook user info" };
                }

                var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
                var userInfo = JsonDocument.Parse(userInfoJson);

                var email = userInfo.RootElement.TryGetProperty("email", out var emailProp) 
                    ? emailProp.GetString() 
                    : null;
                var name = userInfo.RootElement.GetProperty("name").GetString();
                var pictureUrl = userInfo.RootElement.GetProperty("picture")
                    .GetProperty("data")
                    .GetProperty("url").GetString();

                // If no email provided by Facebook, we can't proceed (email is required)
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogError("Facebook user did not provide email");
                    return new AuthResponse { Success = false, Message = "Email permission required" };
                }

                // 3. Check if user exists by FacebookId first, then by email
                var user = await _userRepository.GetByFacebookIdAsync(facebookUserId!);
                
                if (user == null)
                {
                    // Check by email
                    user = await _userRepository.GetByEmailAsync(email);
                    
                    if (user != null)
                    {
                        // Link Facebook ID to existing account
                        user.FacebookId = facebookUserId;
                        await _userRepository.UpdateAsync(user);
                    }
                    else
                    {
                        // 4. Create new user if doesn't exist
                        user = new User
                        {
                            UserName = email,
                            Email = email,
                            DisplayName = name ?? email,
                            Avatar = pictureUrl ?? "https://via.placeholder.com/150",
                            FacebookId = facebookUserId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Status = StatusUser.Online,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                        };
                        await _userRepository.AddAsync(user);
                    }
                }

                // 5. Generate JWT token
                var token = await GenerateJwtTokenAsync(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.Status = StatusUser.Online;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {Email} logged in via Facebook successfully", email);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Facebook login successful",
                    User = await MapToUserAuthDtoAsync(user),
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = DateTime.UtcNow.AddMinutes(30)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Facebook login");
                return new AuthResponse { Success = false, Message = "Internal server error during Facebook login" };
            }
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    return new AuthResponse { Success = false, Message = "Email không tồn tại trong hệ thống" };
                }

                var token = Guid.NewGuid().ToString();
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

                await _userRepository.UpdateAsync(user);

                // Mock URL for now - in real app, get base URL from config
                // Assuming frontend runs on http://localhost:3000
                var resetLink = $"http://localhost:3000/reset-password?token={token}";
                var message = $"<h3>Yêu cầu đặt lại mật khẩu</h3><p>Vui lòng click vào link dưới đây để đặt lại mật khẩu của bạn:</p><a href='{resetLink}'>Đặt lại mật khẩu</a>";

                await _emailService.SendEmailAsync(user.Email!, "Đặt lại mật khẩu ChatApp", message);

                return new AuthResponse { Success = true, Message = "Đã gửi hướng dẫn đặt lại mật khẩu vào email của bạn" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForgotPasswordAsync");
                return new AuthResponse { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var user = await _userRepository.GetByPasswordResetTokenAsync(request.Token);
                if (user == null)
                {
                    return new AuthResponse { Success = false, Message = "Token không hợp lệ hoặc đã hết hạn" };
                }

                if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
                {
                    return new AuthResponse { Success = false, Message = "Token đã hết hạn" };
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                return new AuthResponse { Success = true, Message = "Đặt lại mật khẩu thành công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPasswordAsync");
                return new AuthResponse { Success = false, Message = "Có lỗi xảy ra khi đặt lại mật khẩu" };
            }
        }

        public async Task<EnableTwoFactorResponse?> EnableTwoFactorAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            // Reset key to ensure security (or create if null)
            await _userManager.ResetAuthenticatorKeyAsync(user);
            var key = await _userManager.GetAuthenticatorKeyAsync(user);

            var email = user.Email;
            var appName = "ChatApp";
            var authenticatorUri = $"otpauth://totp/{appName}:{email}?secret={key}&issuer={appName}&digits=6";

            return new EnableTwoFactorResponse
            {
                SharedKey = key!,
                AuthenticatorUri = authenticatorUri
            };
        }

        public async Task<AuthResponse> VerifyTwoFactorSetupAsync(int userId, VerifyTwoFactorRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return new AuthResponse { Success = false, Message = "User not found" };

            // Verify the code against the stored key
            var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.Code);

            if (!isTokenValid)
            {
                return new AuthResponse { Success = false, Message = "Invalid verification code" };
            }

            // Enable 2FA
            await _userManager.SetTwoFactorEnabledAsync(user, true);

            return new AuthResponse { Success = true, Message = "2FA has been enabled" };
        }

        public async Task<bool> DisableTwoFactorAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            return result.Succeeded;
        }

        public async Task<AuthResponse> VerifyTwoFactorLoginAsync(TwoFactorLoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                return new AuthResponse { Success = false, Message = "Invalid username" };
            }

            // Verify the code
            // Note: VerifyTwoFactorTokenAsync validates the code using the AuthenticatorTokenProvider
            var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.Code);

            if (!isTokenValid)
            {
                return new AuthResponse { Success = false, Message = "Invalid 2FA code" };
            }

            // Generate tokens
            var token = await GenerateJwtTokenAsync(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.Status = StatusUser.Online;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                User = await MapToUserAuthDtoAsync(user),
                Token = token,
                RefreshToken = refreshToken,
                ExpiresIn = DateTime.UtcNow.AddMinutes(30)
            };
        }


        private async Task<string> GenerateJwtTokenAsync(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SigningKey"]!));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("DisplayName", user.DisplayName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("Jwt");
                var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SigningKey"]!));

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = secretKey,
                    ValidateLifetime = false
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch
            {
                return null!;
            }
        }

        private async Task<UserAuthDto> MapToUserAuthDtoAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var mainRole = roles.FirstOrDefault() ?? "User";

            return new UserAuthDto
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Avatar = user.Avatar,
                Status = user.Status,
                LastSeenPrivacy = user.LastSeenPrivacy,
                OnlineStatusPrivacy = user.OnlineStatusPrivacy,
                ReadReceiptsEnabled = user.ReadReceiptsEnabled,
                Role = mainRole
            };
        }
    }
}
