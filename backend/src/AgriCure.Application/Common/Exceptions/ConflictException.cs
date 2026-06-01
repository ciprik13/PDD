namespace AgriCure.Application.Common.Exceptions;

public sealed class ConflictException : Exception
{
    public ConflictException()
        : base("The request conflicts with current state.")
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
