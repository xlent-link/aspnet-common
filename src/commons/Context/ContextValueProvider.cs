using System.Collections.Concurrent;

namespace XlentLink.Commons.Context;

public interface IContextValueProvider
{
    T? GetValue<T>(string key);
    void SetValue<T>(string key, T data);

    string? Application { get; set; }
    string? Environment { get; set; }
    string? CorrelationId { get; set; }
}

public class AsyncLocalContextValueProvider : IContextValueProvider
{
    private static readonly ConcurrentDictionary<string, AsyncLocal<object>> Dictionary = new();

    public T? GetValue<T>(string key)
    {
        if (!Dictionary.TryGetValue(key, out AsyncLocal<object>? asyncLocalObject)) return default;
        var o = asyncLocalObject.Value;
        if (o == null) return default;
        return (T)o;
    }

    public void SetValue<T>(string key, T data)
    {
        var addedAsyncLocal = new AsyncLocal<object> { Value = data! };
        if (!Dictionary.TryAdd(key, addedAsyncLocal))
        {
            if (Dictionary.TryGetValue(key, out var existingAsyncLocal))
            {
                existingAsyncLocal.Value = data!;
            }
        }
    }

    public string? Application
    {
        get => GetValue<string?>(nameof(Application));
        set => SetValue(nameof(Application), value);
    }

    public string? Environment
    {
        get => GetValue<string?>(nameof(Environment));
        set => SetValue(nameof(Environment), value);
    }

    public string? CorrelationId
    {
        get => GetValue<string?>(nameof(CorrelationId));
        set => SetValue(nameof(CorrelationId), value);
    }
}