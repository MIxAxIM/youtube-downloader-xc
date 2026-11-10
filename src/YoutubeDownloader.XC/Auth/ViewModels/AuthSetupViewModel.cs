using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Avalonia.Threading;
using PowerKit.Extensions;
using YoutubeDownloader.XC.Framework;
using YoutubeDownloader.XC.Localization;
using YoutubeDownloader.XC.Services;

namespace YoutubeDownloader.XC.Auth;

public class AuthSetupViewModel : DialogViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly IDisposable _eventSubscription;

    public AuthSetupViewModel(
        LocalizationManager localizationManager,
        SettingsService settingsService
    )
    {
        LocalizationManager = localizationManager;
        _settingsService = settingsService;

        _eventSubscription = _settingsService.WatchProperty(
            o => o.LastAuthCookies,
            cookies =>
            {
                OnPropertyChanged(nameof(Cookies));
                OnPropertyChanged(nameof(IsAuthenticated));
                _ = AuthIdentity.RefreshAsync(cookies);
            }
        );
        AuthIdentity.LabelChanged += OnAuthLabelChanged;
        _ = AuthIdentity.RefreshAsync(_settingsService.LastAuthCookies);
    }

    public string AccountLabel => AuthIdentity.Label ?? "";

    private void OnAuthLabelChanged() =>
        Dispatcher.UIThread.Post(() => OnPropertyChanged(nameof(AccountLabel)));

    public LocalizationManager LocalizationManager { get; }

    public IReadOnlyList<Cookie>? Cookies
    {
        get => _settingsService.LastAuthCookies;
        set => _settingsService.LastAuthCookies = value;
    }

    public bool IsAuthenticated
    {
        get
        {
            if (Cookies is null)
                return false;

            // Leftover YouTube cookies are not a login session; require a '__SECURE' cookie.
            var loginCookies = Cookies.Where(c =>
                c.Name.StartsWith("__SECURE", StringComparison.OrdinalIgnoreCase)
            );

            return loginCookies.Any()
                && loginCookies.All(c =>
                    !c.Expired && c.Expires.ToUniversalTime() > DateTime.UtcNow
                );
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AuthIdentity.LabelChanged -= OnAuthLabelChanged;
            _eventSubscription.Dispose();
        }

        base.Dispose(disposing);
    }
}
