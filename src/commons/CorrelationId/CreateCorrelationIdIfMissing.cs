using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using XlentLink.Commons.Context;

namespace XlentLink.Commons.CorrelationId;

public class CreateCorrelationIdIfMissing : IFunctionsWorkerMiddleware
{
    private readonly IContextValueProvider _contextValueProvider;

    public CreateCorrelationIdIfMissing(IContextValueProvider contextValueProvider)
    {
        _contextValueProvider = contextValueProvider;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var request = await context.GetHttpRequestDataAsync();
        string? correlationId = null;
        if (request != null && request.Headers.TryGetValues("X-Correlation-ID", out var headerValues))
        {
            correlationId = headerValues.FirstOrDefault();
        }
        correlationId ??= Guid.NewGuid().ToString();
        _contextValueProvider.CorrelationId = correlationId;

        // Easter egg
        if (correlationId.ToLowerInvariant().Contains("teakettle"))
        {
            var response = request!.CreateResponse((HttpStatusCode)418);
            context.GetInvocationResult().Value = response;
            if (context.GetHttpResponseData() != null) context.GetHttpResponseData()!.StatusCode = (HttpStatusCode)418;
            return;
        }

        await next.Invoke(context);

        context.GetHttpResponseData()?.Headers.Add("X-Correlation-ID", correlationId);
    }
}