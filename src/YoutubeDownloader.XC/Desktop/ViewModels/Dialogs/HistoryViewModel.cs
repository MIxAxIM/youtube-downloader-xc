using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PowerKit.Extensions;
using YoutubeDownloader.XC.Core.Downloading;
using YoutubeDownloader.XC.Framework;
using YoutubeDownloader.XC.Localization;
using YoutubeDownloader.XC.Services;
using YoutubeDownloader.XC.Utils.Extensions;

namespace YoutubeDownloader.XC.ViewModels.Dialogs;

public partial class HistoryViewModel(
    SettingsService settingsService,
    LocalizationManager localizationManager,
    DialogManager dialogManager
) : DialogViewModelBase
{
    public LocalizationManager LocalizationManager { get; } = localizationManager;

    public IReadOnlyList<string> TimeFilters { get; } = ["All", "Today", "7Days", "30Days"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleEntries))]
    public partial string TimeFilter { get; set; } = "All";

    public IReadOnlyList<DownloadHistoryEntry> Entries => settingsService.DownloadHistory;

    public IReadOnlyList<DownloadHistoryEntry> VisibleEntries =>
        QueuePolicy.FilterHistoryByAge(
            Entries,
            entry => entry.CompletedAt,
            TimeFilter,
            DateTimeOffset.Now
        );

    [RelayCommand]
    private void Clear()
    {
        settingsService.DownloadHistory = DownloadHistoryList.Clear();
        OnPropertyChanged(nameof(Entries));
        OnPropertyChanged(nameof(VisibleEntries));
    }

    [RelayCommand]
    private void Delete(DownloadHistoryEntry? entry)
    {
        if (entry is null)
            return;

        settingsService.DownloadHistory = DownloadHistoryList.Delete(Entries, entry);
        OnPropertyChanged(nameof(Entries));
        OnPropertyChanged(nameof(VisibleEntries));
    }

    [RelayCommand]
    private void Open(DownloadHistoryEntry? entry)
    {
        if (string.IsNullOrWhiteSpace(entry?.FilePath))
            return;

        try
        {
            Process.StartShellExecute(entry.FilePath);
        }
        catch
        {
            // The file may have been moved.
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task CopyLinkAsync(DownloadHistoryEntry? entry)
    {
        if (string.IsNullOrWhiteSpace(entry?.Url))
            return;

        if (
            Application.Current?.ApplicationLifetime?.TryGetTopLevel()?.Clipboard
            is not { } clipboard
        )
            return;

        await clipboard.SetTextAsync(entry.Url);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ExportAsync()
    {
        var path = await dialogManager.PromptSaveFilePathAsync(
            [new Avalonia.Platform.Storage.FilePickerFileType("Text") { Patterns = ["*.txt"] }],
            "history.txt"
        );
        if (string.IsNullOrWhiteSpace(path))
            return;

        await File.WriteAllTextAsync(path, DownloadHistoryList.ExportVisible(VisibleEntries));
    }

    [RelayCommand]
    private void Redownload(DownloadHistoryEntry? entry)
    {
        if (string.IsNullOrWhiteSpace(entry?.Url))
            return;

        settingsService.PendingQuery = entry.Url;
        Close(true);
    }
}
