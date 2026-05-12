namespace CIS.BusinessLogic.Exceptions;

public class AuthenticationRequiredException : Exception
{
    public AuthenticationRequiredException(string message) : base(message)
    {
    }
}
