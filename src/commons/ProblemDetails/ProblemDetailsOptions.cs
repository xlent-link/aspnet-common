namespace XlentLink.AspNet.Common.ProblemDetails;

public class ProblemDetailsOptions
{
    /// <summary>
    /// Should be on the form "application-name:environment", e.g. "arctic-tern:prod"
    /// </summary>
    public string ApplicationUrnPart { get; set; } = null!;
}