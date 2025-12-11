using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using uchat_client.Core.Application.Common.Interfaces;

namespace uchat_client.Infrastructure.Services.Storage;

public class SessionStorageService : ISessionStorageService
{
    private readonly string _sessionDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".uchat"
    );

    private string? _instanceId;
    private string? _sessionFile;
    private IDataProtector? _protector;

    private class SessionPayload
    {
        public string SessionToken { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public void Initialize(string clientId)
    {
        _instanceId = clientId;
        _sessionFile = Path.Combine(_sessionDir, $"session-{_instanceId}.dat");

        var services = new ServiceCollection();
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(_sessionDir));

        var serviceProvider = services.BuildServiceProvider();
        var dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
        _protector = dataProtectionProvider.CreateProtector("UchatSessionToken");
    }

    public void SaveSession(string sessionToken, int userId, string username)
    {
        if (_protector == null || _sessionFile == null)
            throw new InvalidOperationException("SessionStorageService not initialized");

        try
        {
            if (!Directory.Exists(_sessionDir))
            {
                Directory.CreateDirectory(_sessionDir);
            }

            var payload = new SessionPayload { SessionToken = sessionToken, UserId = userId, Username = username };
            var json = JsonSerializer.Serialize(payload);
            var protectedToken = _protector.Protect(json);
            File.WriteAllText(_sessionFile, protectedToken);
        }
        catch (Exception)
        {
            // Silently fail - logging will be added by caller if needed
            throw;
        }
    }

    public (string? token, int? userId, string? username) LoadSession()
    {
        if (_protector == null || _sessionFile == null)
            return (null, null, null);

        try
        {
            if (!File.Exists(_sessionFile))
                return (null, null, null);

            var protectedToken = File.ReadAllText(_sessionFile);
            var json = _protector.Unprotect(protectedToken);
            var payload = JsonSerializer.Deserialize<SessionPayload>(json);
            return (payload?.SessionToken, payload?.UserId, payload?.Username);
        }
        catch (Exception)
        {
            return (null, null, null);
        }
    }

    public void ClearSession()
    {
        if (_sessionFile == null)
            return;

        try
        {
            if (File.Exists(_sessionFile))
            {
                File.Delete(_sessionFile);
            }
        }
        catch (Exception)
        {
            // Silently fail
        }
    }

    public string GetDeviceInfo()
    {
        return $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version} - {Environment.MachineName} [{_instanceId ?? "unknown"}]";
    }
}
