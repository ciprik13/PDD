namespace AgriCure.Application.Common.Auth;

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base("Authentication failed.")
    {
    }

    public AuthenticationFailedException(string message)
        : base(message)
    {
    }

    public AuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
