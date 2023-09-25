using XlentLink.AspNet.Common.Context;

namespace XlentLink.AspNet.Common.CorrelationId;

public class CorrelationIdHandler : DelegatingHandler
{
    private readonly IContextValueProvider _contextValueProvider;

    public CorrelationIdHandler(IContextValueProvider contextValueProvider)
    {
        _contextValueProvider = contextValueProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_contextValueProvider.CorrelationId))
        {
            if (!request.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out _))
            {
                request.Headers.Add(CorrelationIdConstants.HeaderName, _contextValueProvider.CorrelationId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}