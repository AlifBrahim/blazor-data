using Microsoft.JSInterop;

namespace Client.Wasm.Services;

public sealed class ConnectivityService : IAsyncDisposable
{
    private readonly IndexedDbOutbox _outbox;
    private DotNetObjectReference<OnlineStateProxy>? _proxy;
    private IJSObjectReference? _subscription;

    public bool IsOnline { get; private set; }

    public event Action<bool>? OnlineStateChanged;

    public ConnectivityService(IndexedDbOutbox outbox)
    {
        _outbox = outbox;
    }

    public async Task InitializeAsync()
    {
        if (_subscription is not null)
        {
            return;
        }

        _proxy = DotNetObjectReference.Create(new OnlineStateProxy(OnStateChanged));
        _subscription = await _outbox.SubscribeOnlineStateAsync(_proxy);
    }

    private void OnStateChanged(bool isOnline)
    {
        IsOnline = isOnline;
        OnlineStateChanged?.Invoke(isOnline);
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscription is not null)
        {
            await _subscription.InvokeVoidAsync("dispose");
            await _subscription.DisposeAsync();
            _subscription = null;
        }

        _proxy?.Dispose();
        _proxy = null;
    }
}
