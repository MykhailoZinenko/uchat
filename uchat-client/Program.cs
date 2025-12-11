using System;
using Avalonia;

namespace uchat_client;

class Program
{
    public static string ServerIp { get; private set; } = string.Empty;
    public static int ServerPort { get; private set; }
    public static string ClientId { get; private set; } = "default";

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            Console.WriteLine("usage: ./uchat server_ip port [client_id]");
            return 1;
        }

        ServerIp = args[0];
        if (!int.TryParse(args[1], out int port))
        {
            Console.WriteLine("Error: Port must be a valid integer");
            return 1;
        }
        ServerPort = port;

        if (args.Length == 3)
        {
            ClientId = args[2];
        }

        Console.WriteLine($"[uchat] Starting client '{ClientId}', connecting to {ServerIp}:{ServerPort}");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
