using uchat_common.Dtos;
using uchat_server.Exceptions;

namespace uchat_server.Services;

public interface IErrorMapper
{
    ApiResponse<T> MapException<T>(Exception ex);
}
