using Core.DTOs.Auth;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<UserAuthDto?> GetCurrentUserAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                return user != null ? MapToUserAuthDto(user) : null;
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

                // Generate tokens
                var token = GenerateJwtToken(user);
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
                    User = MapToUserAuthDto(user),
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = DateTime.UtcNow.AddHours(1)
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

                var newToken = GenerateJwtToken(user);
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
                    User = MapToUserAuthDto(user),
                    RefreshToken = newRefreshToken,
                    ExpiresIn = DateTime.UtcNow.AddHours(1)
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

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {Username} đăng ký thành công", user.UserName);

                return new AuthResponse
                {
                    Success = true,
                    Message = "User registered successfully",
                    User = MapToUserAuthDto(user),
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = DateTime.UtcNow.AddHours(1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký user {Username}", request.Username);
                return new AuthResponse { Success = false, Message = "Error during registration" };
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SigningKey"]!));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName!),
                      new Claim(ClaimTypes.Email, user.Email!),
                new Claim("DisplayName", user.DisplayName)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
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

        private UserAuthDto MapToUserAuthDto(User user)
        {
            return new UserAuthDto
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Avatar = user.Avatar,
                Status = user.Status
            };
        }
    }
}
