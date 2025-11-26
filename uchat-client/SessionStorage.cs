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

    private static readonly string SessionFile = Path.Combine(SessionDir, "session.dat");
    private static readonly IDataProtector _protector;

    static SessionStorage()
    {
        var services = new ServiceCollection();
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(SessionDir));

        var serviceProvider = services.BuildServiceProvider();
        var dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
        _protector = dataProtectionProvider.CreateProtector("UchatSessionToken");
    }

    public static void SaveSession(string sessionToken)
    {
        try
        {
            if (!Directory.Exists(SessionDir))
            {
                Directory.CreateDirectory(SessionDir);
            }

            var protectedToken = _protector.Protect(sessionToken);
            File.WriteAllText(SessionFile, protectedToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save session: {ex.Message}");
        }
    }

    public static string? LoadSession()
    {
        try
        {
            if (!File.Exists(SessionFile))
                return null;

            var protectedToken = File.ReadAllText(SessionFile);
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
        try
        {
            if (File.Exists(SessionFile))
            {
                File.Delete(SessionFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear session: {ex.Message}");
        }
    }

    public static string GetDeviceInfo()
    {
        return $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version} - {Environment.MachineName}";
    }
}
