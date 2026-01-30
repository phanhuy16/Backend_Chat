using Moq;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ChatApp_Backend.Tests.Services
{
    public class MessageServiceTests
    {
        private readonly Mock<IMessageRepository> _messageRepoMock;
        private readonly Mock<IReactionRepository> _reactionRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ILogger<MessageService>> _loggerMock;
        private readonly Mock<IPushNotificationService> _pushServiceMock;
        private readonly Mock<IConversationRepository> _conversationRepoMock;
        private readonly MessageService _messageService;

        public MessageServiceTests()
        {
            _messageRepoMock = new Mock<IMessageRepository>();
            _reactionRepoMock = new Mock<IReactionRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<MessageService>>();
            _pushServiceMock = new Mock<IPushNotificationService>();
            _conversationRepoMock = new Mock<IConversationRepository>();
            _messageService = new MessageService(
                _messageRepoMock.Object,
                _reactionRepoMock.Object,
                _userRepoMock.Object,
                _loggerMock.Object,
                _pushServiceMock.Object,
                _conversationRepoMock.Object
            );
        }

        [Fact]
        public async Task SendMessageAsync_WithParentId_ShouldSaveSuccessfully()
        {
            // Arrange
            int convId = 1;
            int senderId = 1;
            int parentId = 10;
            string content = "Reply message";
            var sender = new User { Id = senderId, DisplayName = "Sender" };

            _userRepoMock.Setup(r => r.GetByIdAsync(senderId)).ReturnsAsync(sender);
            _messageRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => new Message { Id = id, Sender = sender, ParentMessageId = parentId });

            // Act
            var result = await _messageService.SendMessageAsync(convId, senderId, content, MessageType.Text, parentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(parentId, result.ParentMessageId);
            _messageRepoMock.Verify(r => r.AddAsync(It.Is<Message>(m => m.ParentMessageId == parentId)), Times.Once);
        }

        [Fact]
        public async Task TogglePinMessageAsync_ShouldChangePinnedStatus()
        {
            // Arrange
            int messageId = 1;
            int userId = 1;
            int conversationId = 1;
            var message = new Message { Id = messageId, IsPinned = false, ConversationId = conversationId };
            var conversation = new Conversations
            {
                Id = conversationId,
                Members = new List<ConversationMembers>
                {
                    new ConversationMembers { UserId = userId, Role = "Admin", CanPinMessages = true }
                }
            };

            _messageRepoMock.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(message);
            _conversationRepoMock.Setup(r => r.GetConversationWithMembersAsync(conversationId)).ReturnsAsync(conversation);

            // Act
            var result = await _messageService.TogglePinMessageAsync(messageId, userId);

            // Assert
            Assert.True(result);
            Assert.True(message.IsPinned);
            _messageRepoMock.Verify(r => r.UpdateAsync(It.Is<Message>(m => m.IsPinned == true)), Times.Once);
        }

        [Fact]
        public async Task TogglePinMessageAsync_MessageNotFound_ShouldReturnFalse()
        {
            // Arrange
            int messageId = 999;
            int userId = 1;
            _messageRepoMock.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync((Message)null!);

            // Act
            var result = await _messageService.TogglePinMessageAsync(messageId, userId);

            // Assert
            Assert.False(result);
        }
    }
}
