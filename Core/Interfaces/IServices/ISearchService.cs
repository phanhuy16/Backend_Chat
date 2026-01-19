using Core.DTOs.Search;

namespace Core.Interfaces.IServices
{
    public interface ISearchService
    {
        Task<SearchResultDto> SearchAsync(int userId, string query, int pageSize = 20);
        Task<List<MessageSearchResultDto>> SearchMessagesAsync(int userId, string query, int pageSize = 20);
        Task<List<AttachmentSearchResultDto>> SearchFilesAsync(int userId, string query, int pageSize = 20);
        Task<List<UserSearchResultDto>> SearchUsersAsync(int userId, string query, int pageSize = 20);
    }
}
