using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PowerKit.Extensions;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Framework;
using YoutubeDownloader.Localization;
using YoutubeDownloader.Services;
using YoutubeDownloader.Utils.Extensions;
using YoutubeDownloader.ViewModels.Components;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.ViewModels.Dialogs;

public partial class DownloadSingleSetupViewModel(
    ViewModelManager viewModelManager,
    DialogManager dialogManager,
    LocalizationManager localizationManager,
    SettingsService settingsService
) : DialogViewModelBase<DownloadViewModel>
{
    public LocalizationManager LocalizationManager { get; } = localizationManager;

    [ObservableProperty]
    public partial IVideo? Video { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<VideoDownloadOption>? AvailableDownloadOptions { get; set; }

    [ObservableProperty]
    public partial VideoDownloadOption? SelectedDownloadOption { get; set; }

    [ObservableProperty]
    public partial bool IsAudioMode { get; set; }

    [ObservableProperty]
    public partial AudioBitratePreference SelectedAudioBitratePreference { get; set; } =
        AudioBitratePreference.Source;

    public IReadOnlyList<AudioBitratePreference> AvailableAudioBitratePreferences { get; } =
        Enum.GetValues<AudioBitratePreference>();

    public IReadOnlyList<VideoDownloadOption> VisibleDownloadOptions =>
        (AvailableDownloadOptions ?? [])
            .Where(option => option.IsAudioOnly == IsAudioMode)
            .ToArray();

    public override Task InitializeAsync()
    {
        SelectedAudioBitratePreference = settingsService.LastAudioBitratePreference;
        IsAudioMode = settingsService.LastDownloadMediaKind == DownloadMediaKind.Audio;
        SelectMatchingOption();

        return Task.CompletedTask;
    }

    partial void OnAvailableDownloadOptionsChanged(IReadOnlyList<VideoDownloadOption>? value) =>
        OnPropertyChanged(nameof(VisibleDownloadOptions));

    private VideoDownloadOption? _videoSelection;
    private VideoDownloadOption? _audioSelection;

    partial void OnIsAudioModeChanged(bool value)
    {
        OnPropertyChanged(nameof(VisibleDownloadOptions));
        SelectedDownloadOption =
            (value ? _audioSelection : _videoSelection)
            ?? VisibleDownloadOptions.FirstOrDefault(option =>
                option.Container == settingsService.LastContainer
            )
            ?? VisibleDownloadOptions.FirstOrDefault();
    }

    partial void OnSelectedDownloadOptionChanged(VideoDownloadOption? value)
    {
        if (value is null)
            return;

        if (value.IsAudioOnly)
            _audioSelection = value;
        else
            _videoSelection = value;
    }

    private void SelectMatchingOption() => OnIsAudioModeChanged(IsAudioMode);

    [RelayCommand]
    private async Task CopyTitleAsync()
    {
        if (Application.Current?.ApplicationLifetime?.TryGetTopLevel()?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(Video?.Title);
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (Video is null || SelectedDownloadOption is null)
            return;

        var container = SelectedDownloadOption.Container;

        var filePath = await dialogManager.PromptSaveFilePathAsync(
            [
                new FilePickerFileType($"{container.Name} file")
                {
                    Patterns = [$"*.{container.Name}"],
                },
            ],
            FileNameTemplate.Apply(settingsService.FileNameTemplate, Video, container)
        );

        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // Download does not start immediately, so lock in the file path to avoid conflicts
        Directory.CreateForFile(filePath);
        await File.WriteAllBytesAsync(filePath, []);

        settingsService.LastContainer = container;
        settingsService.LastDownloadMediaKind = SelectedDownloadOption.IsAudioOnly
            ? DownloadMediaKind.Audio
            : DownloadMediaKind.Video;
        settingsService.LastAudioBitratePreference = SelectedAudioBitratePreference;

        Close(
            viewModelManager.GetDownloadViewModel(
                Video,
                SelectedDownloadOption,
                filePath,
                SelectedAudioBitratePreference
            )
        );
    }
}
