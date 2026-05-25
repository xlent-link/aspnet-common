using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using XlentLink.AspNet.Common.Context;

namespace XlentLink.AspNet.Common.CorrelationId;

public class CreateCorrelationIdIfMissing : IFunctionsWorkerMiddleware
{
    // The Worker.Extensions.Http.AspNetCore proxying middleware stashes the live HttpContext
    // under this key in FunctionContext.Items.
    private const string AspNetCoreHttpContextKey = "HttpRequestContext";

    private readonly IContextValueProvider _contextValueProvider;

    public CreateCorrelationIdIfMissing(IContextValueProvider contextValueProvider)
    {
        _contextValueProvider = contextValueProvider;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var request = await context.GetHttpRequestDataAsync();
        string? correlationId = null;
        if (request != null && request.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var headerValues))
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

        // Why: Under Worker.Extensions.Http.AspNetCore the response is written through Kestrel
        // inside next(), so by the time we return the response has already started. Touching
        // GetHttpResponseData() then triggers its lazy init -> HttpResponse.OnStarting and throws
        // "OnStarting cannot be set because the response has already started". Register
        // OnStarting now, before next() runs, so the header is appended when Kestrel flushes.
        var aspNetCoreHttpContext = TryGetAspNetCoreHttpContext(context);
        if (aspNetCoreHttpContext != null)
        {
            aspNetCoreHttpContext.Response.OnStarting(() =>
            {
                if (!aspNetCoreHttpContext.Response.Headers.ContainsKey(CorrelationIdConstants.HeaderName))
                {
                    aspNetCoreHttpContext.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
                }
                return Task.CompletedTask;
            });
        }

        await next.Invoke(context);

        // Legacy (non-AspNetCore) worker HTTP mode: response hasn't started yet, safe to mutate here.
        if (aspNetCoreHttpContext == null)
        {
            context.GetHttpResponseData()?.Headers.Add(CorrelationIdConstants.HeaderName, correlationId);
        }
    }

    private static HttpContext? TryGetAspNetCoreHttpContext(FunctionContext context)
    {
        return context.Items.TryGetValue(AspNetCoreHttpContextKey, out var value)
            ? value as HttpContext
            : null;
    }
}