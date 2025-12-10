using Avalonia;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_client;
using uchat_common.Dtos;

class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var appArgs = args;
        Task.Run(() => RunConsoleClientAsync(appArgs));

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        return 0;
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static async Task<int> RunConsoleClientAsync(string[] args)
    {
        if (args.Length < 2)
{
            Console.WriteLine("Usage: uchat <server_ip> <port> [instance_id]");
            return 1;
        }

        string serverIp = args[0];

        if (!int.TryParse(args[1], out int port) || port < 1 || port > 65535)
        {
            Console.WriteLine("Error: Invalid port number. Port must be between 1 and 65535.");
            return 1;
        }

        string instanceId = args.Length > 2 ? args[2] : "default";
        SessionStorage.Initialize(instanceId);

        var reconnectPolicy = new RetryPolicy();
        var sessionRevokedFlag = new System.Threading.Tasks.TaskCompletionSource<bool>();

        var connection = new HubConnectionBuilder()
            .WithUrl($"http://{serverIp}:{port}/chat")
            .WithAutomaticReconnect(reconnectPolicy)
            .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
            .WithServerTimeout(TimeSpan.FromSeconds(30))
            .Build();

        connection.On<MessageDto>("ReceiveMessage", (messageDto) =>
        {
            Console.WriteLine($"[{messageDto.ConnectionId}@{messageDto.Username}]: {messageDto.Content}");
        });

        connection.On<string>("SessionRevoked", (message) =>
        {
            sessionRevokedFlag.TrySetResult(true);
            SessionStorage.ClearSession();
            Console.WriteLine($"\n>>> {message}");
            Console.WriteLine(">>> You have been logged out. Type anything and press Enter to return to login.");
        });

        connection.Reconnecting += error =>
        {
            Console.WriteLine($"Connection lost. Reconnecting... ({error?.Message ?? "Unknown error"})");
            return Task.CompletedTask;
        };

        connection.Reconnected += async connectionId =>
        {
            Console.WriteLine($"Reconnected successfully. Connection ID: {connectionId}");

            var savedSession = SessionStorage.LoadSession();
            if (!string.IsNullOrEmpty(savedSession))
            {
                try
                {
                    var reloginResult = await connection.InvokeAsync<LoginResult>("LoginWithSession", savedSession);
                    if (reloginResult.Success)
                    {
                        Console.WriteLine("[DEBUG] Re-authenticated with saved session");
                    }
                    else
                    {
                        Console.WriteLine("[DEBUG] Session expired, you'll need to login again");
                        SessionStorage.ClearSession();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Re-authentication failed: {ex.Message}");
                }
            }
        };

        connection.Closed += async error =>
        {
            Console.WriteLine($"[DEBUG] Connection closed. {(error != null ? $"Error: {error.Message}" : "")}");
            Console.WriteLine("[DEBUG] Starting manual reconnection loop...");

            int attemptCount = 0;
            await Task.Delay(5000);

            while (true)
            {
                attemptCount++;
                Console.WriteLine($"[DEBUG] Reconnection attempt #{attemptCount} at {DateTime.Now:HH:mm:ss}");

                try
                {
                    await connection.StartAsync();
                    Console.WriteLine($"[DEBUG] Successfully reconnected to server after {attemptCount} attempts!");

                    var savedSession = SessionStorage.LoadSession();
                    if (!string.IsNullOrEmpty(savedSession))
                    {
                        var reloginResult = await connection.InvokeAsync<LoginResult>("LoginWithSession", savedSession);
                        if (reloginResult.Success)
                        {
                            Console.WriteLine("[DEBUG] Re-authenticated with saved session");
                        }
                        else
                        {
                            Console.WriteLine("[DEBUG] Session expired, you'll need to login again");
                            SessionStorage.ClearSession();
                        }
                    }

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Attempt #{attemptCount} failed: {ex.Message}");
                    Console.WriteLine($"[DEBUG] Waiting 5 seconds before next attempt...");
                    await Task.Delay(5000);
                }
            }
        };

        try
        {
            await connection.StartAsync();
            Console.WriteLine("Connected to server successfully!");
            Console.WriteLine();

            while (true)
            {
                LoginResult? loginResult = null;

                var savedSession = SessionStorage.LoadSession();
                if (!string.IsNullOrEmpty(savedSession))
                {
                    Console.WriteLine("Attempting to login with saved session...");
                    loginResult = await connection.InvokeAsync<LoginResult>("LoginWithSession", savedSession);

                    if (!loginResult.Success)
                    {
                        Console.WriteLine("Saved session is invalid or expired.");
                        SessionStorage.ClearSession();
                        loginResult = null;
                    }
                }

                if (loginResult == null)
                {
                    Console.Write("Username: ");
                    string? username = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        Console.WriteLine("Username cannot be empty.");
                        continue;
                    }

                    Console.Write("Password: ");
                    string? password = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        Console.WriteLine("Password cannot be empty.");
                        continue;
                    }

                    var deviceInfo = SessionStorage.GetDeviceInfo();
                    loginResult = await connection.InvokeAsync<LoginResult>("Login", username, password, deviceInfo);

                    if (!loginResult.Success)
                    {
                        Console.WriteLine($"Login failed: {loginResult.Message}");
                        Console.WriteLine();
                        continue;
                    }

                    SessionStorage.SaveSession(loginResult.SessionToken);
                }

                Console.WriteLine($"{loginResult.Message}");
                Console.WriteLine();

                if (loginResult.MessageHistory.Count > 0)
                {
                    Console.WriteLine("=== Message History ===");
                    foreach (var msg in loginResult.MessageHistory)
                    {
                        Console.WriteLine($"[{msg.Username}] {msg.Content} ({msg.SentAt:HH:mm:ss})");
                    }
                    Console.WriteLine("=======================");
                    Console.WriteLine();
                }

                Console.WriteLine("Type your messages and press Enter to send.");
                Console.WriteLine("Commands: /logout, /exit, /sessions, /sessions -r <number>");
                Console.WriteLine();

                bool shouldLogout = false;
                bool shouldExit = false;
                List<SessionInfo>? currentSessions = null;
                sessionRevokedFlag = new System.Threading.Tasks.TaskCompletionSource<bool>();

                while (true)
                {
                    string? input = Console.ReadLine();

                    if (sessionRevokedFlag.Task.IsCompleted)
                    {
                        shouldLogout = true;
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    var trimmedInput = input.Trim();

                    if (trimmedInput.StartsWith('/'))
                    {
                        var commandParts = trimmedInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var command = commandParts[0].ToLower();

                        if (command == "/exit")
                        {
                            shouldExit = true;
                            break;
                        }

                        if (command == "/logout")
                        {
                            shouldLogout = true;
                            await connection.InvokeAsync("Logout");
                            SessionStorage.ClearSession();
                            Console.WriteLine("Logged out.");
                            Console.WriteLine();
                            break;
                        }

                        if (command == "/sessions")
                        {
                            if (commandParts.Length > 1 && commandParts[1] == "-r" && commandParts.Length > 2)
                            {
                                if (currentSessions == null || currentSessions.Count == 0)
                                {
                                    Console.WriteLine("No sessions loaded. Use /sessions first to view sessions.");
                                    continue;
                                }

                                if (int.TryParse(commandParts[2], out int sessionIndex) &&
                                    sessionIndex >= 1 && sessionIndex <= currentSessions.Count)
                                {
                                    var sessionToRevoke = currentSessions[sessionIndex - 1];
                                    var success = await connection.InvokeAsync<bool>("RevokeSession", sessionToRevoke.Token);

                                    if (success)
                                    {
                                        Console.WriteLine($"Session #{sessionIndex} revoked successfully.");
                                        currentSessions = null;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Failed to revoke session #{sessionIndex}.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Invalid session number. Use a number between 1 and {currentSessions.Count}.");
                                }
                            }
                            else
                            {
                                currentSessions = await connection.InvokeAsync<List<SessionInfo>>("GetActiveSessions");

                                if (currentSessions.Count == 0)
                                {
                                    Console.WriteLine("No active sessions.");
                                }
                                else
                                {
                                    Console.WriteLine($"\n=== Active Sessions ({currentSessions.Count}) ===");
                                    for (int i = 0; i < currentSessions.Count; i++)
                                    {
                                        var session = currentSessions[i];
                                        var isCurrent = session.Token == loginResult.SessionToken;
                                        var marker = isCurrent ? " (current)" : "";
                                        Console.WriteLine($"{i + 1}. {session.DeviceInfo}{marker}");
                                        Console.WriteLine($"   Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                                        Console.WriteLine($"   Last Active: {session.LastActivityAt:yyyy-MM-dd HH:mm:ss}");
                                        Console.WriteLine($"   Expires: {session.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
                                    }
                                    Console.WriteLine("===========================\n");
                                }
                            }
                            continue;
                        }

                        Console.WriteLine($"Unknown command: {command}");
                        continue;
                    }

                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, Console.CursorTop);

                    await connection.InvokeAsync("SendMessage", trimmedInput);
                }

                if (shouldExit)
                    break;

                if (!shouldLogout)
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }

        return 0;
    }
}
