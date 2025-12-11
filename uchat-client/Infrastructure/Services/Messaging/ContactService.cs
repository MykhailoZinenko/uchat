using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_common.Dtos;

namespace uchat_client.Infrastructure.Services.Messaging;

public class ContactService : IContactService
{
    private readonly IHubConnectionService _hubConnection;
    private readonly IAuthService _authService;
    private readonly ILoggingService _logger;

    public event EventHandler<UserDto>? ContactAdded;
    public event EventHandler<int>? ContactRemoved;

    public ContactService(
        IHubConnectionService hubConnection,
        IAuthService authService,
        ILoggingService logger)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to SignalR events for contact updates if needed
        // _hubConnection.Connection.On<UserDto>("ContactAdded", user => ContactAdded?.Invoke(this, user));
        // _hubConnection.Connection.On<int>("ContactRemoved", userId => ContactRemoved?.Invoke(this, userId));
    }

    public async Task<ApiResponse<List<UserDto>>> SearchUsersAsync(string query)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("SearchUsersAsync called without an authenticated session");
            return new ApiResponse<List<UserDto>> { Success = false, Message = "Not authenticated" };
        }

        _logger.LogDebug("Searching users: {Query}", query);
        return await _hubConnection.InvokeAsync<ApiResponse<List<UserDto>>>(
            "SearchUsers",
            token,
            query);
    }

    public async Task<ApiResponse<List<UserDto>>> GetContactsAsync()
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GetContactsAsync called without an authenticated session");
            return new ApiResponse<List<UserDto>> { Success = false, Message = "Not authenticated" };
        }

        _logger.LogDebug("Fetching contacts");

        // For now, return empty list as there's no contacts concept yet
        // In future, implement GetContacts endpoint on server
        return await Task.FromResult(new ApiResponse<List<UserDto>>
        {
            Success = true,
            Data = new List<UserDto>(),
            Message = "Contacts feature not yet implemented"
        });
    }

    public async Task<ApiResponse<bool>> AddContactAsync(string username)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("AddContactAsync called without an authenticated session");
            return new ApiResponse<bool> { Success = false, Message = "Not authenticated" };
        }

        _logger.LogDebug("Adding contact: {Username}", username);

        // For now, return success without actual implementation
        // In future, implement AddContact endpoint on server
        return await Task.FromResult(new ApiResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Contact feature not yet implemented"
        });
    }

    public async Task<ApiResponse<bool>> RemoveContactAsync(int userId)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("RemoveContactAsync called without an authenticated session");
            return new ApiResponse<bool> { Success = false, Message = "Not authenticated" };
        }

        _logger.LogDebug("Removing contact: {UserId}", userId);

        // For now, return success without actual implementation
        // In future, implement RemoveContact endpoint on server
        return await Task.FromResult(new ApiResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Contact feature not yet implemented"
        });
    }
}
