using System.Linq;
using System.Net;
using Domain;
using Microsoft.Extensions.Logging;

namespace Client.Wasm.Services;

public sealed class SyncService
{
    private readonly IndexedDbOutbox _outbox;
    private readonly ConnectivityService _connectivity;
    private readonly DeviceInfoService _deviceInfo;
    private readonly UserProfileService _profileService;
    private readonly ApiClient _apiClient;
    private readonly ILogger<SyncService> _logger;

    public SyncService(IndexedDbOutbox outbox,
        ConnectivityService connectivity,
        DeviceInfoService deviceInfo,
        UserProfileService profileService,
        ApiClient apiClient,
        ILogger<SyncService> logger)
    {
        _outbox = outbox;
        _connectivity = connectivity;
        _deviceInfo = deviceInfo;
        _profileService = profileService;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<int> GetPendingCountAsync() => await _outbox.PendingCountAsync();

    public async Task EnqueueOrSyncAsync(ProductEntry entry, CancellationToken cancellationToken = default)
    {
        entry.DeviceId = await _deviceInfo.GetOrCreateDeviceIdAsync();
        entry.UserId = await EnsureUserIdAsync() ?? entry.UserId;
        entry.CapturedAt = entry.CapturedAt == default ? DateTimeOffset.UtcNow : entry.CapturedAt;

        if (!_connectivity.IsOnline)
        {
            await _outbox.QueueAsync(entry);
            return;
        }

        var sent = await TrySendAsync(entry, cancellationToken);
        if (!sent)
        {
            await _outbox.QueueAsync(entry);
        }
    }

    public async Task<int> SyncPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _outbox.ReadPendingAsync();
        var processed = 0;

        foreach (var item in pending.OrderBy(x => x.EnqueuedAt ?? DateTimeOffset.UtcNow))
        {
            var candidate = ToEntry(item);
            if (string.IsNullOrWhiteSpace(candidate.UserId))
            {
                candidate.UserId = await EnsureUserIdAsync() ?? candidate.UserId;
            }

            if (!await TrySendAsync(candidate, cancellationToken))
            {
                continue;
            }

            await _outbox.RemoveAsync(item.Id);
            processed++;
        }

        return processed;
    }

    private async Task<string?> EnsureUserIdAsync()
    {
        var cached = await _profileService.GetUserIdAsync();
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        if (_connectivity.IsOnline)
        {
            return await _profileService.RefreshAsync();
        }

        return null;
    }

    private async Task<bool> TrySendAsync(ProductEntry entry, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var status = await _apiClient.PostAsync("api/entries", entry);
        if (status.Ok || status.Status == (int)HttpStatusCode.Conflict)
        {
            return true;
        }

        if (status.Status >= 500)
        {
            _logger.LogWarning("Server error while syncing entry {EntryId}. Status code: {Status}", entry.Id, status.Status);
        }
        else if (status.Status == (int)HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Unauthorized while syncing entry {EntryId}. Ensure the user is authenticated.", entry.Id);
            await _profileService.RefreshAsync();
        }
        else
        {
            _logger.LogWarning("Failed to sync entry {EntryId}. Status code: {Status}", entry.Id, status.Status);
        }

        return false;
    }

    private static ProductEntry ToEntry(OutboxDocument document) => new()
    {
        Id = document.Id,
        Name = document.Name,
        CapturedAt = document.CapturedAt,
        ProductModel = document.ProductModel,
        PartNumber = document.PartNumber,
        Quantity = document.Quantity,
        Price = document.Price,
        DeviceId = document.DeviceId,
        UserId = document.UserId
    };
}
