using uchat_common.Dtos;
using uchat_server.Data.Entities;

namespace uchat_server.Services;

public class MapperService : IMapperService
{
    public UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            StatusText = user.StatusText,
            IsOnline = user.IsOnline,
            LastSeenAt = user.LastSeenAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
