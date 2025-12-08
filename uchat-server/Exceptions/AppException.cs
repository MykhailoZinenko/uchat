namespace uchat_server.Exceptions;

public class AppException : Exception
{
    public AppException(string message) : base(message)
    {
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class ValidationException : AppException
{
    public ValidationException(string message) : base(message)
    {
    }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
