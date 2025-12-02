using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using uchat_server.Data;
using uchat_server.Hubs;
using uchat_server.Repositories;
using uchat_server.Services;

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

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

builder.Services.AddDbContext<UchatDbContext>(options =>
    options.UseSqlite("Data Source=uchat.db"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IMapperService, MapperService>();

builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UchatDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors();
app.MapHub<ChatHub>("/chat");

Console.WriteLine($"Server PID: {Environment.ProcessId}");
Console.WriteLine($"Server listening on port {port}");
Console.WriteLine($"SignalR Hub available at: http://localhost:{port}/chat");

await app.RunAsync();

return 0;
