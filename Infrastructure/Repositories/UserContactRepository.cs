using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserContactRepository : Repository<UserContact>, IUserContactRepository
    {
        public UserContactRepository(ChatAppDbContext context) : base(context) { }

        // Check if two users are friends
        public async Task<bool> AreFriendsAsync(int userId1, int userId2)
        {
            return await _context.UserContacts
               .AnyAsync(uc =>
                   (uc.SenderId == userId1 && uc.ReceiverId == userId2 ||
                    uc.SenderId == userId2 && uc.ReceiverId == userId1) &&
                   uc.Status == StatusFriend.Accepted); // Status = Accepted
        }

        // Get friend request between two users
        public async Task<UserContact?> GetFriendRequestAsync(int senderId, int receiverId)
        {
            return await _context.UserContacts
               .Include(uc => uc.Sender)
               .Include(uc => uc.Receiver)
               .FirstOrDefaultAsync(uc =>
                   (uc.SenderId == senderId && uc.ReceiverId == receiverId) ||
                   (uc.SenderId == receiverId && uc.ReceiverId == senderId));
        }

        // Get accepted friends for a user
        public async Task<IEnumerable<UserContact>> GetFriendsAsync(int userId)
        {
            return await _context.UserContacts
                .Include(uc => uc.Sender)
                .Include(uc => uc.Receiver)
                .Where(uc =>
                    (uc.SenderId == userId || uc.ReceiverId == userId) &&
                    uc.Status == StatusFriend.Accepted) // Status = Accepted
                .OrderByDescending(uc => uc.BecomeFriendAt)
                .ToListAsync();
        }

        // Get pending friend requests for a user (received requests)
        public async Task<IEnumerable<UserContact>> GetPendingRequestsAsync(int userId)
        {
            return await _context.UserContacts
                .Include(uc => uc.Sender)
                .Include(uc => uc.Receiver)
                .Where(uc => uc.ReceiverId == userId && uc.Status == StatusFriend.Pending)
                .OrderByDescending(uc => uc.CreatedAt)
                .ToListAsync();
        }

        // Get sent friend requests for a user
        public async Task<IEnumerable<UserContact>> GetSentRequestsAsync(int userId)
        {
            return await _context.UserContacts
                .Include(uc => uc.Sender)
                .Include(uc => uc.Receiver)
                .Where(uc => uc.SenderId == userId && uc.Status == StatusFriend.Pending)
                .OrderByDescending(uc => uc.CreatedAt)
                .ToListAsync();
        }

        // Get all contacts (both sent and received) for a user
        public async Task<IEnumerable<UserContact>> GetUserContactsAsync(int userId)
        {
            return await _context.UserContacts
                 .Include(uc => uc.Sender)
                 .Include(uc => uc.Receiver)
                 .Where(uc => uc.SenderId == userId || uc.ReceiverId == userId)
                 .OrderByDescending(uc => uc.CreatedAt)
                 .ToListAsync();
        }
    }
}
