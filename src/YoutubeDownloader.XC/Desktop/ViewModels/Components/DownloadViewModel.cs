using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gress;
using PowerKit.Extensions;
using YoutubeDownloader.XC.Core.Downloading;
using YoutubeDownloader.XC.Framework;
using YoutubeDownloader.XC.Localization;
using YoutubeDownloader.XC.Utils.Extensions;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.XC.ViewModels.Components;

public partial class DownloadViewModel : ViewModelBase
{
    private readonly ViewModelManager _viewModelManager;
    private readonly DialogManager _dialogManager;

    private readonly IDisposable _eventSubscription;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _isDisposed;

    public DownloadViewModel(
        ViewModelManager viewModelManager,
        DialogManager dialogManager,
        LocalizationManager localizationManager
    )
    {
        _viewModelManager = viewModelManager;
        _dialogManager = dialogManager;
        LocalizationManager = localizationManager;

        _eventSubscription = Progress.WatchProperty(
            o => o.Current,
            _ =>
            {
                OnPropertyChanged(nameof(IsProgressIndeterminate));
                UpdateProgressSnapshot();
            }
        );
    }

    public LocalizationManager LocalizationManager { get; }

    [ObservableProperty]
    public partial IVideo? Video { get; set; }

    [ObservableProperty]
    public partial VideoDownloadOption? DownloadOption { get; set; }

    [ObservableProperty]
    public partial VideoDownloadPreference? DownloadPreference { get; set; }

    [ObservableProperty]
    public partial AudioBitratePreference AudioBitrate { get; set; } =
        AudioBitratePreference.Source;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCanceledOrFailed))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenFileCommand))]
    public partial DownloadStatus Status { get; set; } = DownloadStatus.Ready;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyErrorMessageCommand))]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError =>
        Status is DownloadStatus.Failed || !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool NeedsAuthentication => Video is Core.Resolving.PreviewVideo;

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public string? FileName => Path.GetFileName(FilePath);

    public ProgressContainer<Percentage> Progress { get; } = new();

    public bool IsProgressIndeterminate => Progress.Current.Fraction is <= 0 or >= 1;

    public bool IsCanceledOrFailed => Status is DownloadStatus.Canceled or DownloadStatus.Failed;

    private bool CanCancel() => Status is DownloadStatus.Enqueued or DownloadStatus.Started;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        if (_isDisposed)
            return;

        _cancellationTokenSource.Cancel();
    }

    private bool CanShowFile() =>
        Status == DownloadStatus.Completed
        // This only works on Windows currently
        && OperatingSystem.IsWindows();

    [RelayCommand(CanExecute = nameof(CanShowFile))]
    private async Task ShowFileAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            return;

        try
        {
            // Navigate to the file in Windows Explorer
            Process.Start("explorer", ["/select,", FilePath]);
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelManager.GetMessageBoxViewModel(LocalizationManager.ErrorTitle, ex.Message)
            );
        }
    }

    private bool CanOpenFile() => Status == DownloadStatus.Completed;

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task OpenFileAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            return;

        try
        {
            Process.StartShellExecute(FilePath);
            Played = true;
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelManager.GetMessageBoxViewModel(LocalizationManager.ErrorTitle, ex.Message)
            );
        }
    }

    [RelayCommand]
    private async Task CopyErrorMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ErrorMessage))
            return;

        if (Application.Current?.ApplicationLifetime?.TryGetTopLevel()?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(ErrorMessage);
    }

    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _eventSubscription.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
