using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Material.Styles.Themes;
using Microsoft.Extensions.DependencyInjection;
using PowerKit.Extensions;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Framework;
using YoutubeDownloader.Localization;
using YoutubeDownloader.Services;
using YoutubeDownloader.Utils.Extensions;
using YoutubeDownloader.ViewModels;
using YoutubeDownloader.ViewModels.Components;
using YoutubeDownloader.ViewModels.Dialogs;

namespace YoutubeDownloader;

public partial class App : Application, IDisposable
{
    private readonly ServiceProvider _services;
    private readonly SettingsService _settingsService;

    private readonly IDisposable _eventSubscription;

    private bool _isDisposed;

    public App()
    {
        var services = new ServiceCollection();

        // Framework
        services.AddSingleton<DialogManager>();
        services.AddSingleton<SnackbarManager>();
        services.AddSingleton<ViewManager>();
        services.AddSingleton<ViewModelManager>();

        // Localization
        services.AddSingleton<LocalizationManager>();

        // Services
        services.AddSingleton<SettingsService>();
        services.AddSingleton<UpdateService>();

        // View models
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DownloadViewModel>();
        services.AddTransient<AuthSetupViewModel>();
        services.AddTransient<DownloadMultipleSetupViewModel>();
        services.AddTransient<DownloadSingleSetupViewModel>();
        services.AddTransient<MessageBoxViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<SettingsViewModel>();

        _services = services.BuildServiceProvider(true);
        _settingsService = _services.GetRequiredService<SettingsService>();

        // Re-initialize the theme when the user changes it
        _eventSubscription = _settingsService.WatchProperty(
            o => o.Theme,
            v =>
            {
                RequestedThemeVariant = v switch
                {
                    ThemeVariant.Light => Avalonia.Styling.ThemeVariant.Light,
                    ThemeVariant.Dark => Avalonia.Styling.ThemeVariant.Dark,
                    _ => Avalonia.Styling.ThemeVariant.Default,
                };

                InitializeTheme();
            }
        );
    }

    private void InitializeTheme()
    {
        var actualTheme = RequestedThemeVariant?.Key switch
        {
            "Light" => PlatformThemeVariant.Light,
            "Dark" => PlatformThemeVariant.Dark,
            _ => PlatformSettings?.GetColorValues().ThemeVariant ?? PlatformThemeVariant.Light,
        };

        this.LocateMaterialTheme<MaterialThemeBase>().CurrentTheme =
            actualTheme == PlatformThemeVariant.Light
                ? Theme.Create(Theme.Light, Color.Parse("#343838"), Color.Parse("#F9A825"))
                : Theme.Create(Theme.Dark, Color.Parse("#E8E8E8"), Color.Parse("#F9A825"));
    }

    public override void Initialize()
    {
        base.Initialize();

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Load settings
        _settingsService.Load();
        _settingsService.ApplyProxy();
        _settingsService.ApplySpeedLimit();

        // Initialize and configure the main window
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewManager = _services.GetRequiredService<ViewManager>();
            var viewModelManager = _services.GetRequiredService<ViewModelManager>();

            desktop.MainWindow = viewManager.TryBindWindow(viewModelManager.GetMainViewModel());

            // Although `App.Dispose()` is invoked from `Program.Main(...)`, on some platforms
            // it may be called too late in the shutdown lifecycle. Attach an exit
            // handler to ensure timely disposal as a safeguard.
            // https://github.com/Tyrrrz/YoutubeDownloader/issues/795
            desktop.Exit += (_, _) => Dispose();
        }

        // Initialize the theme for the first time; must be done after the main window is created
        InitializeTheme();

        base.OnFrameworkInitializationCompleted();
    }

    private void Application_OnActualThemeVariantChanged(object? sender, EventArgs args) =>
        // Re-initialize the theme when the system theme changes
        InitializeTheme();

    private void TrayOpen_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        desktop.MainWindow?.Show();
        if (desktop.MainWindow is not null)
            desktop.MainWindow.WindowState = WindowState.Normal;
        desktop.MainWindow?.Activate();
    }

    private void TrayPauseResume_OnClick(object? sender, EventArgs e)
    {
        if (
            ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime
            {
                MainWindow.DataContext: MainViewModel main
            }
        )
            return;

        foreach (
            var card in main.Dashboard.Downloads.Where(card =>
                card.Status == DownloadStatus.Started
            )
        )
        {
            if (card.IsPaused)
                card.ResumeDownloadCommand.Execute(null);
            else
                card.PauseDownloadCommand.Execute(null);
        }
    }

    private void TrayQuit_OnClick(object? sender, EventArgs e) => Shutdown();

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _eventSubscription.Dispose();
        _services.Dispose();
    }
}

public partial class App
{
    public static void Shutdown(int exitCode = 0)
    {
        if (Current?.ApplicationLifetime?.TryShutdown(exitCode) != true)
            Environment.Exit(exitCode);
    }
}
