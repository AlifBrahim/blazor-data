using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Client.Wasm.Services;

public sealed class UserProfileService
{
    private const string UserSettingsKey = "userId";

    private readonly IndexedDbOutbox _outbox;
    private readonly ApiClient _apiClient;
    private readonly NavigationManager _navigation;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(IndexedDbOutbox outbox, ApiClient apiClient, NavigationManager navigation, ILogger<UserProfileService> logger)
    {
        _outbox = outbox;
        _apiClient = apiClient;
        _navigation = navigation;
        _logger = logger;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var cached = await _outbox.GetSettingAsync(UserSettingsKey);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        return await RefreshAsync();
    }

    public async Task<string?> RefreshAsync()
    {
        var response = await _apiClient.GetAsync<UserProfileResponse>("api/users/me");
        if (response.Ok && response.Data?.Id is { Length: > 0 } id)
        {
            await _outbox.SetSettingAsync(UserSettingsKey, id);
            return id;
        }

        if (response.Status == (int)HttpStatusCode.Unauthorized)
        {
            RedirectToLogin();
        }
        else
        {
            _logger.LogWarning("Unable to refresh user profile. Status code {Status}", response.Status);
        }
        return null;
    }

    private sealed record UserProfileResponse(string Id, string Email, string[] Roles);

    private void RedirectToLogin()
    {
        var target = Uri.EscapeDataString(_navigation.Uri);
        var returnUrl = Uri.EscapeDataString($"/auth/continue?target={target}");
        var loginUrl = $"{_apiClient.BaseOrigin}/Identity/Account/Login?returnUrl={returnUrl}";
        _navigation.NavigateTo(loginUrl, forceLoad: true);
    }
}
