using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using uchat_server.Configuration;
using uchat_server.Data;
using uchat_server.Hubs;
using uchat_server.Repositories;
using uchat_server.Services;
using uchat_server.Data.Entities;

if (args.Length == 0)
{
    Console.WriteLine("Usage: uchat_server <port>");
    return 1;
}

if (!int.TryParse(args[0], out int port) || port < 1 || port > 65535)
{
    Console.WriteLine("Error: Invalid port number. Port must be between 1 and 65535.");
    return 1;
}

var contentRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                  ?? Directory.GetCurrentDirectory();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot
});

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Logging
    .ClearProviders()
    .AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(LogLevel.Debug)
    .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning)
    .AddFilter("Microsoft", LogLevel.Debug)
    .AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug)
    .AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Debug);

// Настройка конфигурации из appsettings.json с валидацией
builder.Services.AddOptions<SessionSettings>()
    .Bind(builder.Configuration.GetSection("Session"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DatabaseSettings>()
    .Bind(builder.Configuration.GetSection("Database"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<UchatDbContext>((serviceProvider, options) =>
{
    DatabaseSettings? dbSettings = builder.Configuration.GetSection("Database").Get<DatabaseSettings>();
    var connectionString = dbSettings?.ConnectionString ?? "Data Source=uchat.db";

    const string prefix = "Data Source=";
    if (connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        var dbFile = connectionString[prefix.Length..].Trim();
        if (!Path.IsPathRooted(dbFile))
        {
            dbFile = Path.Combine(contentRoot, dbFile);
        }

        var dbDir = Path.GetDirectoryName(dbFile);
        if (!string.IsNullOrEmpty(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        connectionString = $"{prefix}{dbFile}";
    }

    Console.WriteLine($"Using SQLite at: {connectionString}");

    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomMemberRepository, RoomMemberRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageEditRepository, MessageEditRepository>();
builder.Services.AddScoped<IMessageDeletionRepository, MessageDeletionRepository>();

builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<ICryptographyService, CryptographyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IRoomMemberService, RoomMemberService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IMapperService, MapperService>();
builder.Services.AddScoped<IErrorMapper, ErrorMapper>();

builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
}).AddHubOptions<ChatHub>(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.AddSingleton<IHubFilter, LoggingHubFilter>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UchatDbContext>();
    await db.Database.MigrateAsync();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var rooms = await db.Rooms
        .Include(r => r.Members)
        .ThenInclude(rm => rm.User)
        .ToListAsync();
    if (rooms.Count == 0)
    {
        logger.LogWarning("Startup: No rooms found in DB");
    }
    else
    {
        foreach (var room in rooms)
        {
            logger.LogInformation("Startup: Room Id={Id} Name={Name} IsGlobal={IsGlobal} CreatedBy={CreatedByUserId}", room.Id, room.RoomName ?? "(null)", room.IsGlobal, room.CreatedByUserId);

            var members = room.Members?.Where(rm => rm.LeftAt == null).ToList() ?? new List<RoomMember>();
            if (members.Count == 0)
            {
                logger.LogWarning("Startup: Room Id={Id} has no active members", room.Id);
            }
            else
            {
                foreach (var member in members)
                {
                    logger.LogInformation("Startup: Room Id={RoomId} Member UserId={UserId} Username={Username}", room.Id, member.UserId, member.User?.Username ?? "(unknown)");
                }
            }
        }
    }
}

app.UseCors();
app.MapHub<ChatHub>("/chat");

Console.WriteLine($"Server PID: {Environment.ProcessId}");
Console.WriteLine($"Server listening on port {port}");
Console.WriteLine($"SignalR Hub available at: http://localhost:{port}/chat");

await app.RunAsync();

return 0;
