using System.Text.Json.Serialization;
using Domain;
using Microsoft.JSInterop;

namespace Client.Wasm.Services;

public sealed class IndexedDbOutbox : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public IndexedDbOutbox(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/outbox.js").AsTask());
    }

    public async Task<Guid> QueueAsync(ProductEntry entry)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<Guid>("queueEntry", entry);
    }

    public async Task RemoveAsync(Guid id)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("dequeueEntry", id);
    }

    public async Task<IReadOnlyList<OutboxDocument>> ReadPendingAsync()
    {
        var module = await _moduleTask.Value;
        var items = await module.InvokeAsync<OutboxDocument[]>("readPending");
        return items;
    }

    public async Task<int> PendingCountAsync()
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<int>("pendingCount");
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string?>("getSetting", key);
    }

    public async Task SetSettingAsync(string key, string value)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setSetting", key, value);
    }

    public async ValueTask<IJSObjectReference> SubscribeOnlineStateAsync(DotNetObjectReference<OnlineStateProxy> proxy)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<IJSObjectReference>("subscribeOnlineStatus", proxy);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}

public sealed class OutboxDocument
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("capturedAt")]
    public DateTimeOffset CapturedAt { get; set; }

    [JsonPropertyName("productModel")]
    public string ProductModel { get; set; } = string.Empty;

    [JsonPropertyName("partNumber")]
    public string PartNumber { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("enqueuedAt")]
    public DateTimeOffset? EnqueuedAt { get; set; }
}

public sealed class OnlineStateProxy
{
    private readonly Action<bool> _callback;

    public OnlineStateProxy(Action<bool> callback)
    {
        _callback = callback;
    }

    [JSInvokable]
    public void UpdateOnlineState(bool online) => _callback(online);
}
