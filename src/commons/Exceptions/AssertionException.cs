namespace XlentLink.AspNet.Common.Exceptions;

public class AssertionException : Exception
{
    public AssertionException(string message)
    : base(message)
    {
    }
}