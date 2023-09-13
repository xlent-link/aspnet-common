using System.Net;
using System.Security.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using XlentLink.Commons.Context;
using XlentLink.Commons.Exceptions;
using XlentLink.Commons.Logging;

namespace XlentLink.Commons.ProblemDetails;

public class ProblemDetailsMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ISyncLogger _logger;
    private readonly ProblemDetailsOptions _options;
    private readonly IContextValueProvider _contextValueProvider;

    public ProblemDetailsMiddleware(ISyncLoggerFactory loggerFactory, ProblemDetailsOptions options, IContextValueProvider contextValueProvider)
    {
        _options = options;
        _contextValueProvider = contextValueProvider;
        _logger = loggerFactory.CreateForCategory(nameof(ProblemDetailsMiddleware));
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Catch any exception and convert it to ProblemDetails
        try
        {
            await next.Invoke(context);
        }
        catch (Exception e)
        {
            if (e is AggregateException) e = e.InnerException!;

            // Setup by other middleware
            var correlationId = _contextValueProvider.CorrelationId ?? "unknown";

            // Each request is a new instance
            var instanceId = Guid.NewGuid();
            var problemDetails = new ProblemDetails
            {
                Detail = e.Message,
                Instance = $"urn:{_options.ApplicationUrnPart}:instance:{instanceId}:correlation-id:{correlationId}"
            };

            // Inspect exception an d decide which type of error it is
            ResolveErrorType(problemDetails, e);

            // Log and return problem details response
            _logger.LogError("Exception thrown", e, data: new Dictionary<string, object?>
            {
                { nameof(problemDetails), problemDetails }
            });

            var request = await context.GetHttpRequestDataAsync();
            var response = request!.CreateResponse((HttpStatusCode)problemDetails.Status);
            await response.WriteAsJsonAsync(problemDetails);
            context.GetInvocationResult().Value = response;
            if (context.GetHttpResponseData() != null) context.GetHttpResponseData()!.StatusCode = (HttpStatusCode)problemDetails.Status;
        }
    }

    private static void ResolveErrorType(ProblemDetails problemDetails, Exception e)
    {
        // BusinessRuleException is used for business rules
        // ArgumentException is used for validation
        if (e is BusinessRuleException or ArgumentException)
        {
            problemDetails.Status = 400;
            problemDetails.Title = "Bad request, ";
            if (e is BusinessRuleException) problemDetails.Title += "business rule failure";
            else if (e is ArgumentException) problemDetails.Title += "validation error";
            problemDetails.Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/400";
        }

        // ResourceNotFoundException is used when resource can not be found
        else if (e is ResourceNotFoundException)
        {
            problemDetails.Status = 404;
            problemDetails.Title = "Resource not found";
            problemDetails.Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/404";
        }

        // AuthenticationException is used for authentication failure
        else if (e is AuthenticationException)
        {
            problemDetails.Status = 401;
            problemDetails.Title = "Unauthorized";
            problemDetails.Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/401";
        }

        // TODO: 409 Conflict

        // TODO: Maybe 429 Too many requests?

        // Everything else is considered Internal Server Error
        else
        {
            problemDetails.Status = 500;
            problemDetails.Title = "Internal server error";
            problemDetails.Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/500";
        }
    }
}