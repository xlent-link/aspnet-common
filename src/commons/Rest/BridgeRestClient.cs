using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Nexus.Link.Bridge.Plugin.CSharp.Target.Abstract.Rest;
using XlentLink.AspNet.Common.Exceptions;

namespace XlentLink.AspNet.Common.Rest;

public class BridgeRestClient : IBridgeRestClient
{
    private readonly HttpClient _httpClient;
    
    private readonly JsonSerializerOptions _serializeOptions;
    private readonly JsonSerializerOptions _deserializeOptions;

    private static readonly JsonSerializerOptions DefaultSerializeOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions DefaultDeserializeOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters = { new JsonStringEnumConverter() }
    };

    public BridgeRestClient(HttpClient httpClient) : this(httpClient, DefaultSerializeOptions, DefaultDeserializeOptions)
    {
    }

    public BridgeRestClient(HttpClient httpClient, JsonSerializerOptions serializeOptions, JsonSerializerOptions deserializeOptions)
    {
        _httpClient = httpClient;
        _serializeOptions = serializeOptions;
        _deserializeOptions = deserializeOptions;
    }

    public async Task<TReturnValue> PostAsync<TReturnValue, TContent>(string url, TContent item, CancellationToken cancellationToken = default)
    {
        var requestContent = new StringContent(JsonSerializer.Serialize(item, _serializeOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, requestContent, cancellationToken);
        await VerifyResponseAsync(url, response, cancellationToken);
        var result = await ConvertResponseToResultAsync<TReturnValue>(response, cancellationToken);
        return result;
    }

    public async Task<TReturnValue> GetAsync<TReturnValue>(string url, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        await VerifyResponseAsync(url, response, cancellationToken);
        var result = await ConvertResponseToResultAsync<TReturnValue>(response, cancellationToken);
        return result!;
    }

    public async Task<TReturnValue> PutAsync<TReturnValue, TContent>(string url, TContent item, CancellationToken cancellationToken = default)
    {
        var requestContent = new StringContent(JsonSerializer.Serialize(item, _serializeOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.PutAsync(url, requestContent, cancellationToken);
        await VerifyResponseAsync(url, response, cancellationToken);
        var result = await ConvertResponseToResultAsync<TReturnValue>(response, cancellationToken);
        return result;
    }

    public async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(url, cancellationToken);
        await VerifyResponseAsync(url, response, cancellationToken);
    }

    private async Task VerifyResponseAsync(string url, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var responseMessage = await response.Content.ReadAsStringAsync(cancellationToken);
            _httpClient.BaseAddress ??= new Uri("https://example.com/");
            var fullUrl = $"{_httpClient.BaseAddress}{(_httpClient.BaseAddress.ToString().EndsWith("/") ? "" : "/")}{(url.StartsWith("/") ? url[1..] : url)}";
            throw new UnsuccessfulHttpResponseException(fullUrl, response.StatusCode, responseMessage);
        }
    }

    private async Task<T> ConvertResponseToResultAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NoContent) return default!;

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (responseContent == null!) return default!;
        
        var result = JsonSerializer.Deserialize<T>(responseContent, _deserializeOptions);
        return result ?? default!;
    }
}