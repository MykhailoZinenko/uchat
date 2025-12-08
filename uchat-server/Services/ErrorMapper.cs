using uchat_common.Dtos;
using uchat_server.Exceptions;

namespace uchat_server.Services;

public class ErrorMapper : IErrorMapper
{
    public ApiResponse<T> MapException<T>(Exception ex)
    {
        return ex switch
        {
            ValidateAccessTokenException => new ApiResponse<T>
            {
                Success = false,
                Message = "Invalid or expired access token"
            },
            ValidateRefreshTokenException => new ApiResponse<T>
            {
                Success = false,
                Message = "Invalid or expired refresh token"
            },
            AppException appEx => new ApiResponse<T>
            {
                Success = false,
                Message = appEx.Message
            },
            _ => new ApiResponse<T>
            {
                Success = false,
                Message = "Internal server error"
            }
        };
    }
}
