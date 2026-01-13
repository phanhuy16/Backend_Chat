using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByPasswordResetTokenAsync(string token);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
    }
}
