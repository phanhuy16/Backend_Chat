using Core.DTOs.Blocks;
using Core.Entities;
using Core.Interfaces.IRepositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlocksController : ControllerBase
    {
        private readonly IBlockedUserRepository _blockedUserRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<BlocksController> _logger;

        public BlocksController(
            IBlockedUserRepository blockedUserRepository,
            IUserRepository userRepository,
            ILogger<BlocksController> logger)
        {
            _blockedUserRepository = blockedUserRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet("check/{blockerId}/{blockedUserId}")]
        public async Task<IActionResult> IsUserBlocked(int blockerId, int blockedUserId)
        {
            try
            {
                var blockRecord = await _blockedUserRepository.GetAsync(blockerId, blockedUserId);
                return Ok(blockRecord != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking block status");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetBlockedUsers(int userId)
        {
            try
            {
                var blockedUsers = await _blockedUserRepository.GetBlockedUsersAsync(userId);
                
                var dtos = blockedUsers.Select(b => new BlockedUserDto
                {
                    Id = b.Id,
                    BlockerId = b.BlockerId,
                    BlockedUserId = b.BlockedUserId,
                    CreatedAt = b.CreatedAt,
                    BlockedUserProfile = b.BlockedUserProfile != null ? new BlockedUserProfileDto
                    {
                        Id = b.BlockedUserProfile.Id,
                        UserName = b.BlockedUserProfile.UserName ?? "",
                        DisplayName = b.BlockedUserProfile.DisplayName,
                        Avatar = b.BlockedUserProfile.Avatar,
                        Status = b.BlockedUserProfile.Status
                    } : null
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blocked users");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BlockUser([FromBody] BlockUserRequest request)
        {
            try
            {
                if (request.BlockerId == request.BlockedUserId)
                    return BadRequest("Cannot block yourself");

                var blocker = await _userRepository.GetByIdAsync(request.BlockerId);
                var blockedUser = await _userRepository.GetByIdAsync(request.BlockedUserId);

                if (blocker == null || blockedUser == null)
                    return NotFound("User not found");

                var existing = await _blockedUserRepository.GetAsync(request.BlockerId, request.BlockedUserId);
                if (existing != null)
                    return BadRequest("User already blocked");

                var blockRecord = new BlockedUser
                {
                    BlockerId = request.BlockerId,
                    BlockedUserId = request.BlockedUserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _blockedUserRepository.AddAsync(blockRecord);
                _logger.LogInformation("User {UserId} blocked by {BlockerId}", request.BlockedUserId, request.BlockerId);

                return Ok("User blocked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking user");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{blockerId}/{blockedUserId}")]
        public async Task<IActionResult> UnblockUser(int blockerId, int blockedUserId)
        {
            try
            {
                var blockRecord = await _blockedUserRepository.GetAsync(blockerId, blockedUserId);
                if (blockRecord == null)
                    return NotFound("Block record not found");

                await _blockedUserRepository.DeleteAsync(blockRecord);
                _logger.LogInformation("User {UserId} unblocked by {BlockerId}", blockedUserId, blockerId);

                return Ok("User unblocked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking user");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("check-mutual/{userId1}/{userId2}")]
        public async Task<IActionResult> IsUserBlockedMutual(int userId1, int userId2)
        {
            try
            {
                // Kiểm tra cả hai hướng block
                var isBlocked = await _blockedUserRepository.IsUserBlockedAsync(userId1, userId2);
                var blockInfo = isBlocked
                    ? await _blockedUserRepository.GetBlockerInfoAsync(userId1, userId2)
                    : null;

                return Ok(new
                {
                    isBlocked = isBlocked,
                    // Return blocker ID so UI knows who blocked whom
                    blockerId = blockInfo?.BlockerId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking mutual block status");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
