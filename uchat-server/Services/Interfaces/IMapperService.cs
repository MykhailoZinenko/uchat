using uchat_common.Dtos;
using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface IMapperService
{
    UserDto MapToUserDto(User user);
}
