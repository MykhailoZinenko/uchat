namespace uchat_client.Core.Application.Common.Interfaces;

public interface ISessionStorageService
{
    void Initialize(string clientId);
    void SaveSession(string sessionToken, int userId, string username);
    (string? token, int? userId, string? username) LoadSession();
    void ClearSession();
    string GetDeviceInfo();
}
