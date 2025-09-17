namespace Client.Wasm.Services;

public sealed class DeviceInfoService
{
    private readonly IndexedDbOutbox _outbox;

    private const string DeviceKey = "deviceId";

    public DeviceInfoService(IndexedDbOutbox outbox)
    {
        _outbox = outbox;
    }

    public async Task<string> GetOrCreateDeviceIdAsync()
    {
        var existing = await _outbox.GetSettingAsync(DeviceKey);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var generated = $"device-{Guid.NewGuid():N}";
        await _outbox.SetSettingAsync(DeviceKey, generated);
        return generated;
    }
}
