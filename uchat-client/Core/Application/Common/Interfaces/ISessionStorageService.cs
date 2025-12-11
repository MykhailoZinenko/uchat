namespace uchat_client.Core.Application.Common.Interfaces;

public interface ISessionStorageService
{
    void Initialize(string clientId);
    void SaveSession(string sessionToken, string username);
    (string? token, string? username) LoadSession();
    void ClearSession();
    string GetDeviceInfo();
}
