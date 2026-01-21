using Moq;
using Core.Entities;
using Core.DTOs.Users;
using Core.Interfaces.IRepositories;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ChatApp_Backend.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<UserService>>();
            _userService = new UserService(
                _userRepoMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task UpdateCustomStatusAsync_ShouldUpdateSuccessfully()
        {
            // Arrange
            int userId = 1;
            string newStatus = "Feeling good";
            var user = new User { Id = userId, CustomStatus = "Old status" };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _userService.UpdateCustomStatusAsync(userId, newStatus);

            // Assert
            Assert.True(result);
            Assert.Equal(newStatus, user.CustomStatus);
            _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.CustomStatus == newStatus)), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldUpdateBioAndDisplayName()
        {
            // Arrange
            int userId = 1;
            var request = new UpdateProfileRequest 
            { 
                DisplayName = "New Name", 
                Bio = "New Bio" 
            };
            var user = new User { Id = userId, DisplayName = "Old Name", Bio = "Old Bio" };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _userService.UpdateProfileAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", user.DisplayName);
            Assert.Equal("New Bio", user.Bio);
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }
    }
}
