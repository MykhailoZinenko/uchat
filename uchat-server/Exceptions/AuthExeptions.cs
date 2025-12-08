namespace uchat_server.Exceptions;

public class ValidateRefreshTokenException : AppException
{
    public ValidateRefreshTokenException(string message) : base(message)
    {
    }
}

public class ValidateAccessTokenException : AppException
{
    public ValidateAccessTokenException(string message) : base(message)
    {
    }
}


