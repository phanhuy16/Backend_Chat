using Core.DTOs.Auth;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký user mới
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid registration request");
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("User registration attempt: {Username}", request.Username);

                var result = await _authService.RegisterAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("Registration failed for {Username}", request.Username);
                    return BadRequest(result);
                }

                _logger.LogInformation("Registration successful for {Username}", request.Username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Username}", request.Username);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Đăng nhập user
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid login request");
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("User login attempt: {Username}", request.Username);

                var result = await _authService.LoginAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("Login failed for {Username}", request.Username);
                    return BadRequest(result);
                }

                _logger.LogInformation("Login successful for {Username}", request.Username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Username}", request.Username);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Refresh JWT token
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid refresh token request");
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new { success = false, message = "Token and RefreshToken are required" });
                }

                var result = await _authService.RefreshTokenAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("Refresh token failed");
                    return Unauthorized(result);
                }

                _logger.LogInformation("Token refreshed successfully");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Logout user
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                {
                    _logger.LogWarning("Logout attempt failed: User not found");
                    return Unauthorized("User not found");
                }

                _logger.LogInformation("User logout attempt: {UserId}", userId);

                var result = await _authService.LogoutAsync(userId);

                if (!result)
                {
                    _logger.LogWarning("Logout failed for {UserId}", userId);
                    return BadRequest("Logout failed");
                }

                _logger.LogInformation("Logout successful for {UserId}", userId);
                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
                return BadRequest("IdToken is required");

            var result = await _authService.LoginWithGoogleAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("facebook-login")]
        [AllowAnonymous]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.AccessToken))
                return BadRequest("AccessToken is required");

            var result = await _authService.LoginWithFacebookAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Get current user info
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                {
                    _logger.LogWarning("GetCurrentUser failed: User not found");
                    return Unauthorized("User not found");
                }

                var user = await _authService.GetCurrentUserAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("GetCurrentUser failed: User not found in service");
                    return NotFound("User not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Enable 2FA
        /// </summary>
        [HttpPost("enable-2fa")]
        [Authorize]
        public async Task<IActionResult> EnableTwoFactor()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized("User not found");

                var result = await _authService.EnableTwoFactorAsync(userId);
                if (result == null) return BadRequest("Could not generate 2FA key");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling 2FA");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Verify 2FA Setup
        /// </summary>
        [HttpPost("verify-2fa-setup")]
        [Authorize]
        public async Task<IActionResult> VerifyTwoFactorSetup([FromBody] VerifyTwoFactorRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized("User not found");

                var result = await _authService.VerifyTwoFactorSetupAsync(userId, request);
                if (!result.Success) return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying 2FA setup");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Disable 2FA
        /// </summary>
        [HttpPost("disable-2fa")]
        [Authorize]
        public async Task<IActionResult> DisableTwoFactor()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized("User not found");

                var result = await _authService.DisableTwoFactorAsync(userId);
                if (!result) return BadRequest("Failed to disable 2FA");

                return Ok(new { success = true, message = "2FA disabled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling 2FA");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Verify 2FA during Login
        /// </summary>
        [HttpPost("verify-2fa-login")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyTwoFactorLogin([FromBody] TwoFactorLoginRequest request)
        {
            try
            {
                var result = await _authService.VerifyTwoFactorLoginAsync(request);
                if (!result.Success) return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying 2FA login");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Verify token
        /// </summary>
        [HttpGet("verify")]
        [Authorize]
        public IActionResult VerifyToken()
        {
            try
            {
                var userId = GetCurrentUserId();
                var username = User.FindFirst(ClaimTypes.Name)?.Value;

                return Ok(new
                {
                    valid = true,
                    userId,
                    username
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying token");
                return StatusCode(500, "Internal server error");
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}
