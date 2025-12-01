using Core.DTOs.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.IServices
{
    public interface IChatHubService
    {
        Task AddUserConnectionAsync(int userId, int conversationId, string connectionId);
        Task RemoveUserConnectionAsync(int userId, int conversationId, string connectionId);
        Task RemoveAllUserConnectionsAsync(string connectionId);
        Task<IEnumerable<UserDto>> GetOnlineUsersInConversationAsync(int conversationId);
        Task<int> GetUserConnectionCountAsync(int userId);
    }
}
