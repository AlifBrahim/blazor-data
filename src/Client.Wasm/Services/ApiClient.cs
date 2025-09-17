using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Client.Wasm.Services;

public sealed class ApiClient : IAsyncDisposable
{
    private readonly Uri _baseUri;
    private readonly Lazy<Task<IJSObjectReference>> _module;

    public ApiClient(IJSRuntime jsRuntime, string baseUrl)
    {
        _baseUri = new Uri(baseUrl, UriKind.Absolute);
        _module = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/apiClient.js").AsTask());
    }

    public async Task<ApiResponse<T?>> GetAsync<T>(string path)
    {
        var module = await _module.Value;
        var js = await module.InvokeAsync<JsResponse>("getJson", BaseUrl, path);
        if (!js.Ok || string.IsNullOrWhiteSpace(js.Body))
        {
            return new ApiResponse<T?>(js.Ok, js.Status, default);
        }

        var value = JsonSerializer.Deserialize<T>(js.Body, JsonSerializerOptions);
        return new ApiResponse<T?>(js.Ok, js.Status, value);
    }

    public async Task<ApiStatus> PostAsync<T>(string path, T payload)
    {
        var module = await _module.Value;
        var js = await module.InvokeAsync<JsResponse>("postJson", BaseUrl, path, payload);
        return new ApiStatus(js.Ok, js.Status);
    }

    public string BaseUrl => _baseUri.ToString().TrimEnd('/');

    public string BaseOrigin => _baseUri.GetLeftPart(UriPartial.Authority);

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            var module = await _module.Value;
            await module.DisposeAsync();
        }
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private sealed record JsResponse([property: JsonPropertyName("ok")] bool Ok,
                                     [property: JsonPropertyName("status")] int Status,
                                     [property: JsonPropertyName("body")] string? Body);
}

public sealed record ApiStatus(bool Ok, int Status);

public sealed record ApiResponse<T>(bool Ok, int Status, T? Data);
