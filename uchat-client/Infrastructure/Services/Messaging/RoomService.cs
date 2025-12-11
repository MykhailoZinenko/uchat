using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_common.Dtos;

namespace uchat_client.Infrastructure.Services.Messaging;

public class RoomService : IRoomService
{
    private readonly IHubConnectionService _hubConnection;
    private readonly IAuthService _authService;
    private readonly ILoggingService _logger;

    public RoomService(
        IHubConnectionService hubConnection,
        IAuthService authService,
        ILoggingService logger)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<List<RoomDto>>> GetAccessibleRoomsAsync()
    {
        if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.SessionToken))
        {
            _logger.LogWarning("GetAccessibleRoomsAsync called without an authenticated session");
            return new ApiResponse<List<RoomDto>>
            {
                Success = false,
                Message = "Not authenticated"
            };
        }

        _logger.LogDebug("Fetching accessible rooms");
        return await _hubConnection.InvokeAsync<ApiResponse<List<RoomDto>>>(
            "GetAccessibleRooms",
            _authService.SessionToken!);
    }

    public async Task<ApiResponse<RoomDto>> CreateRoomAsync(string type, string? name, string? description)
    {
        if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.SessionToken))
        {
            _logger.LogWarning("CreateRoomAsync called without an authenticated session");
            return new ApiResponse<RoomDto>
            {
                Success = false,
                Message = "Not authenticated"
            };
        }

        _logger.LogDebug("Creating room: Type={Type} Name={Name}", type, name);
        return await _hubConnection.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom",
            _authService.SessionToken!,
            type,
            name,
            description);
    }

    public async Task<ApiResponse<bool>> JoinRoomAsync(int roomId)
    {
        if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.SessionToken))
        {
            _logger.LogWarning("JoinRoomAsync called without an authenticated session");
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Not authenticated"
            };
        }

        _logger.LogDebug("Joining room: RoomId={RoomId}", roomId);
        return await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "JoinRoom",
            _authService.SessionToken!,
            roomId);
    }

    public async Task<ApiResponse<bool>> LeaveRoomAsync(int roomId)
    {
        if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.SessionToken))
        {
            _logger.LogWarning("LeaveRoomAsync called without an authenticated session");
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Not authenticated"
            };
        }

        _logger.LogDebug("Leaving room: RoomId={RoomId}", roomId);
        return await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "LeaveRoom",
            _authService.SessionToken!,
            roomId);
    }

    public async Task<ApiResponse<bool>> AddRoomMembersAsync(int roomId, List<int> userIds)
    {
        if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.SessionToken))
        {
            _logger.LogWarning("AddRoomMembersAsync called without an authenticated session");
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Not authenticated"
            };
        }

        _logger.LogDebug("Adding members to room: RoomId={RoomId} Count={Count}", roomId, userIds.Count);
        return await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "AddRoomMembers",
            _authService.SessionToken!,
            roomId,
            userIds);
    }

    public async Task<ApiResponse<RoomDto>> UpdateRoomAsync(int roomId, string? name, string? description, string? avatarUrl)
    {
        if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.SessionToken))
        {
            _logger.LogWarning("UpdateRoomAsync called without an authenticated session");
            return new ApiResponse<RoomDto>
            {
                Success = false,
                Message = "Not authenticated"
            };
        }

        _logger.LogDebug("Updating room: RoomId={RoomId}", roomId);
        return await _hubConnection.InvokeAsync<ApiResponse<RoomDto>>(
            "UpdateRoom",
            _authService.SessionToken!,
            roomId,
            name,
            description,
            avatarUrl);
    }
}
