namespace XlentLink.AspNet.Common.Exceptions;

public class ResourceNotFoundException : Exception
{
    public string ResourceId { get; set; } = null!;

    public ResourceNotFoundException(string message)
        : base(message)
    {
    }
}