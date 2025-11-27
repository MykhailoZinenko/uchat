using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace uchat_client;

public static class SessionStorage
{
    private static readonly string SessionDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".uchat"
    );

    private static string? _instanceId;
    private static string? _sessionFile;
    private static IDataProtector? _protector;

    public static void Initialize(string instanceId)
    {
        _instanceId = instanceId;
        _sessionFile = Path.Combine(SessionDir, $"session-{_instanceId}.dat");

        var services = new ServiceCollection();
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(SessionDir));

        var serviceProvider = services.BuildServiceProvider();
        var dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
        _protector = dataProtectionProvider.CreateProtector("UchatSessionToken");
    }

    public static void SaveSession(string sessionToken)
    {
        if (_protector == null || _sessionFile == null)
            throw new InvalidOperationException("SessionStorage not initialized");

        try
        {
            if (!Directory.Exists(SessionDir))
            {
                Directory.CreateDirectory(SessionDir);
            }

            var protectedToken = _protector.Protect(sessionToken);
            File.WriteAllText(_sessionFile, protectedToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save session: {ex.Message}");
        }
    }

    public static string? LoadSession()
    {
        if (_protector == null || _sessionFile == null)
            return null;

        try
        {
            if (!File.Exists(_sessionFile))
                return null;

            var protectedToken = File.ReadAllText(_sessionFile);
            return _protector.Unprotect(protectedToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load session: {ex.Message}");
            return null;
        }
    }

    public static void ClearSession()
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
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear session: {ex.Message}");
        }
    }

    public static string GetDeviceInfo()
    {
        return $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version} - {Environment.MachineName} [{_instanceId ?? "unknown"}]";
    }
}
