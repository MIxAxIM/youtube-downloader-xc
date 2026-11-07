using System;
using System.Threading.Tasks;
using Avalonia;
using YoutubeDownloader.XC.Framework;
using YoutubeDownloader.XC.Localization;
using YoutubeDownloader.XC.Services;
using YoutubeDownloader.XC.Utils.Extensions;
using YoutubeDownloader.XC.ViewModels.Components;

namespace YoutubeDownloader.XC.ViewModels;

public partial class MainViewModel(
    ViewModelManager viewModelManager,
    DialogManager dialogManager,
    SnackbarManager snackbarManager,
    LocalizationManager localizationManager,
    SettingsService settingsService,
    UpdateService updateService
) : ViewModelBase
{
    // dialogManager kept for DI parity with other shell view models
    private readonly DialogManager _dialogManager = dialogManager;
    private readonly SettingsService _settingsService = settingsService;

    public string Title { get; } = $"{Program.DisplayName} v{Program.VersionString}";

    public string VersionLabel { get; } = $"v{Program.VersionString}";

    public SettingsService Settings => _settingsService;

    public DashboardViewModel Dashboard { get; } = viewModelManager.GetDashboardViewModel();

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var updateVersion = await updateService.CheckForUpdatesAsync();
            if (updateVersion is null)
                return;

            snackbarManager.Notify(
                string.Format(
                    localizationManager.UpdateDownloadingMessage,
                    Program.Name,
                    updateVersion
                )
            );
            await updateService.PrepareUpdateAsync(updateVersion);

            snackbarManager.Notify(
                localizationManager.UpdateReadyMessage,
                localizationManager.UpdateInstallNowButton,
                () =>
                {
                    updateService.FinalizeUpdate(true);

                    if (Application.Current?.ApplicationLifetime?.TryShutdown(2) != true)
                        Environment.Exit(2);
                }
            );
        }
        catch
        {
            // Failure to update shouldn't crash the application
            snackbarManager.Notify(localizationManager.UpdateFailedMessage);
        }
    }

    public async Task<bool> ConfirmQuitWhileDownloadingAsync()
    {
        var dialog = viewModelManager.GetMessageBoxViewModel(
            localizationManager.ErrorTitle,
            "Downloads are still running. Quit anyway?",
            localizationManager.OkButton,
            localizationManager.CancelButton
        );
        return await _dialogManager.ShowDialogAsync(dialog) == true;
    }

    public override async Task InitializeAsync()
    {
        await CheckForUpdatesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Save settings
            _settingsService.Save();

            // Finalize pending updates
            updateService.FinalizeUpdate(false);
        }

        base.Dispose(disposing);
    }
}
