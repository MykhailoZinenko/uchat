using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface ISearchService
{
    Task<List<User>> SearchUsersAsync(string query, int limit = 20);
    Task<List<Message>> SearchMessagesAsync(string query, int userId, int? roomId = null, int limit = 50);
}
