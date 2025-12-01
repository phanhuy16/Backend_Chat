using Core.DTOs.Users;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class ChatHubService : IChatHubService
    {
        private static Dictionary<string, UserConnection> _connections = new Dictionary<string, UserConnection>();
        private static readonly object _lockObject = new object();
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ChatHubService> _logger;
        public ChatHubService(IUserRepository userRepository, ILogger<ChatHubService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }
        public async Task AddUserConnectionAsync(int userId, int conversationId, string connectionId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_connections.ContainsKey(connectionId))
                    {
                        _logger.LogWarning("Kết nối {ConnectionId} đã tồn tại, tiến hành replace.", connectionId);
                        _connections.Remove(connectionId);
                    }

                    _connections[connectionId] = new UserConnection
                    {
                        UserId = userId,
                        ConversationId = conversationId,
                        ConnectionId = connectionId,
                        ConnectedAt = DateTime.UtcNow
                    };
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.Status = StatusUser.Online;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);

                    _logger.LogInformation("User {UserId} đã online (Conn: {ConnId})", userId, connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi AddUserConnectionAsync (UserId: {UserId}, Conn: {ConnId})", userId, connectionId);
            }
        }

        public async Task<IEnumerable<UserDto>> GetOnlineUsersInConversationAsync(int conversationId)
        {
            try
            {
                List<int> onlineUserIds;

                lock (_lockObject)
                {
                    onlineUserIds = _connections.Values
                        .Where(c => c.ConversationId == conversationId)
                        .Select(c => c.UserId)
                        .Distinct()
                        .ToList();
                }

                var users = new List<UserDto>();

                foreach (var userId in onlineUserIds)
                {
                    var user = await _userRepository.GetByIdAsync(userId);

                    if (user != null)
                    {
                        users.Add(new UserDto
                        {
                            Id = user.Id,
                            UserName = user.UserName!,
                            DisplayName = user.DisplayName,
                            Avatar = user.Avatar,
                            Status = StatusUser.Online
                        });
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách online trong conversation {ConversationId}", conversationId);
                return Enumerable.Empty<UserDto>();
            }
        }

        public async Task<int> GetUserConnectionCountAsync(int userId)
        {
            try
            {
                lock (_lockObject)
                {
                    return _connections.Values.Count(c => c.UserId == userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi GetUserConnectionCountAsync user {UserId}", userId);
                return 0;
            }
        }

        public async Task RemoveAllUserConnectionsAsync(string connectionId)
        {
            UserConnection? connection = null;

            try
            {
                lock (_lockObject)
                {
                    _connections.TryGetValue(connectionId, out connection);
                    if (connection != null)
                    {
                        _connections.Remove(connectionId);
                        _logger.LogInformation("Đã xóa connection {ConnId}", connectionId);
                    }
                }

                if (connection != null)
                {
                    await RemoveUserConnectionAsync(connection.UserId, connection.ConversationId, connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tại RemoveAllUserConnectionsAsync (Conn: {ConnId})", connectionId);
            }
        }

        public async Task RemoveUserConnectionAsync(int userId, int conversationId, string connectionId)
        {
            try
            {
                bool hasOtherConnections;

                lock (_lockObject)
                {
                    if (_connections.ContainsKey(connectionId))
                    {
                        _connections.Remove(connectionId);
                    }

                    hasOtherConnections = _connections.Values
                        .Any(c => c.UserId == userId);
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null && !hasOtherConnections)
                {
                    user.Status = StatusUser.Offline;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);

                    _logger.LogInformation("User {UserId} đã offline (cuối connection)", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi RemoveUserConnectionAsync User: {UserId}, Conn: {ConnId}",
                    userId, connectionId);
            }
        }

        public IEnumerable<UserConnection> GetUserConnections(int userId)
        {
            try
            {
                lock (_lockObject)
                {
                    return _connections.Values
                        .Where(c => c.UserId == userId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetUserConnections user {UserId}", userId);
                return Enumerable.Empty<UserConnection>();
            }
        }

        public IEnumerable<UserConnection> GetConversationConnections(int conversationId)
        {
            try
            {
                lock (_lockObject)
                {
                    return _connections.Values
                        .Where(c => c.ConversationId == conversationId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetConversationConnections conv {ConversationId}", conversationId);
                return Enumerable.Empty<UserConnection>();
            }
        }

        public IEnumerable<UserConnection> GetAllConnections()
        {
            try
            {
                lock (_lockObject)
                {
                    return _connections.Values.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetAllConnections");
                return Enumerable.Empty<UserConnection>();
            }
        }
    }
}
