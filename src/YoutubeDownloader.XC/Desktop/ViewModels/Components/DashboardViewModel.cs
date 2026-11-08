using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gress;
using Gress.Completable;
using PowerKit;
using PowerKit.Extensions;
using YoutubeDownloader.XC.Core.Downloading;
using YoutubeDownloader.XC.Core.Resolving;
using YoutubeDownloader.XC.Core.Tagging;
using YoutubeDownloader.XC.Framework;
using YoutubeDownloader.XC.Localization;
using YoutubeDownloader.XC.Services;
using YoutubeDownloader.XC.Utils.Extensions;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.XC.ViewModels.Components;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ViewModelManager _viewModelManager;
    private readonly SnackbarManager _snackbarManager;
    private readonly DialogManager _dialogManager;
    private readonly LocalizationManager _localizationManager;
    private readonly SettingsService _settingsService;

    private readonly IDisposable _eventSubscription;
    private readonly ResizableSemaphore _downloadSemaphore = new();
    private readonly ResizableSemaphore _lookupSemaphore = new() { MaxCount = 3 };
    private readonly AutoResetProgressMuxer _progressMuxer;
    private DownloadViewModel? _undoCard;
    private int? _undoIndex;
    private readonly DispatcherTimer _filterDebounce = new()
    {
        Interval = TimeSpan.FromMilliseconds(250),
    };

    public DashboardViewModel(
        ViewModelManager viewModelManager,
        SnackbarManager snackbarManager,
        DialogManager dialogManager,
        LocalizationManager localizationManager,
        SettingsService settingsService
    )
    {
        _viewModelManager = viewModelManager;
        _snackbarManager = snackbarManager;
        _dialogManager = dialogManager;
        _localizationManager = localizationManager;
        LocalizationManager = localizationManager;
        _settingsService = settingsService;

        _progressMuxer = Progress.CreateMuxer().WithAutoReset();

        _eventSubscription = Disposable.Merge(
            _settingsService.WatchProperty(
                o => o.ParallelLimit,
                v => _downloadSemaphore.MaxCount = v,
                true
            ),
            Progress.WatchProperty(
                o => o.Current,
                _ => OnPropertyChanged(nameof(IsProgressIndeterminate))
            )
        );

        SpeedLimits =
        [
            new(0, localizationManager.UnlimitedLabel),
            new(256_000, "256 KB/s"),
            new(512_000, "512 KB/s"),
            new(1_000_000, "1 MB/s"),
            new(2_000_000, "2 MB/s"),
            new(5_000_000, "5 MB/s"),
        ];
        LoadDefaults();
        QueueFilter = string.IsNullOrWhiteSpace(_settingsService.LastQueueFilter)
            ? "All"
            : _settingsService.LastQueueFilter;
        QueueSearch = _settingsService.LastQueueSearch;
        IsCompact = _settingsService.CompactCards;

        _speedTimer.Tick += (_, _) =>
        {
            OnPropertyChanged(nameof(SpeedText));
            OnPropertyChanged(nameof(UsageText));
        };
        _speedTimer.Start();
        _filterDebounce.Tick += (_, _) =>
        {
            _filterDebounce.Stop();
            OnPropertyChanged(nameof(VisibleDownloads));
        };
        Downloads.CollectionChanged += (_, _) => OnPropertyChanged(nameof(VisibleDownloads));
        RestorePersistedQueue();
    }

    partial void OnQueueFilterChanged(string value)
    {
        _settingsService.LastQueueFilter = value;
        OnPropertyChanged(nameof(VisibleDownloads));
    }

    partial void OnQueueSearchChanged(string? value)
    {
        _settingsService.LastQueueSearch = value;
        _filterDebounce.Stop();
        _filterDebounce.Start();
    }

    partial void OnIsCompactChanged(bool value)
    {
        _settingsService.CompactCards = value;
        foreach (var card in Downloads)
            card.IsCompact = value;
    }

    private readonly DispatcherTimer _speedTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(500),
    };
    private bool _defaultsReady;

    public LocalizationManager LocalizationManager { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProgressIndeterminate))]
    [NotifyCanExecuteChangedFor(nameof(ProcessQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowAuthSetupCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowSettingsCommand))]
    public partial bool IsBusy { get; set; }

    public ProgressContainer<Percentage> Progress { get; } = new();

    public bool IsProgressIndeterminate => IsBusy && Progress.Current.Fraction is <= 0 or >= 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProcessQueryCommand))]
    public partial string? Query { get; set; }

    public IReadOnlyList<string> QuerySuggestions => _settingsService.QueryHistory;

    public ObservableCollection<DownloadViewModel> Downloads { get; } = [];

    public IEnumerable<DownloadViewModel> VisibleDownloads =>
        Downloads.Where(card =>
            QueuePolicy.IsCardVisible(
                card.EditedTitle.Length > 0 ? card.EditedTitle : card.Video?.Title,
                QueueSearch,
                QueueFilter,
                card.IsAudioMode,
                card.Status.ToString()
            )
        );

    public IReadOnlyList<string> QueueFilters { get; } =
    ["All", "Video", "Audio", "Downloading", "Completed", "Failed"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleDownloads))]
    public partial string QueueFilter { get; set; } = "All";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleDownloads))]
    public partial string? QueueSearch { get; set; }

    [ObservableProperty]
    public partial bool IsCompact { get; set; }

    [ObservableProperty]
    public partial bool IsFilterVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsTopmost { get; set; }

    public bool CanUndoRemove => _undoCard is not null;

    public string UsageText
    {
        get
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            if (_settingsService.BytesDownloadedDay != today)
                return $"Today 0 MB · Total {DownloadProgressMath.FormatMegabytes(_settingsService.BytesDownloadedTotal)}";

            return $"Today {DownloadProgressMath.FormatMegabytes(_settingsService.BytesDownloadedToday)} · Total {DownloadProgressMath.FormatMegabytes(_settingsService.BytesDownloadedTotal)}";
        }
    }

    public IReadOnlyList<SpeedLimitOption> SpeedLimits { get; }

    public IReadOnlyList<BitrateChoice> DefaultBitrateChoices { get; } =
        BitrateChoiceList.Build([]);

    [ObservableProperty]
    public partial bool IsAudioDefault { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormatChoices))]
    public partial Container SelectedFormat { get; set; } = Container.Mp4;

    [ObservableProperty]
    public partial BitrateChoice SelectedDefaultBitrate { get; set; } = BitrateChoice.Source;

    [ObservableProperty]
    public partial SpeedLimitOption? SelectedSpeedLimit { get; set; }

    public IReadOnlyList<Container> FormatChoices => MediaFormats.Containers(IsAudioDefault);

    public string SpeedText =>
        DownloadSpeed.Format(
            DownloadSpeed.Combine(
                Downloads
                    .Where(download => download.Status == DownloadStatus.Started)
                    .Select(download => download.BytesPerSecond)
            )
        );

    private async Task EnsureFFmpegAsync()
    {
        // If a custom path is set, trust that the user knows what they're doing
        if (_settingsService.FFmpegFilePath is not null)
            return;

        // If FFmpeg can be auto-detected, all good
        if (FFmpeg.TryGetCliFilePath() is not null)
            return;

        // Otherwise, prompt the user to download FFmpeg
        var dialog = _viewModelManager.GetMessageBoxViewModel(
            _localizationManager.FFmpegMissingTitle,
            string.Format(_localizationManager.FFmpegMissingMessage, Program.Name),
            _localizationManager.DownloadButton,
            _localizationManager.CloseButton
        );

        // If the user declined, open settings to nudge them to set a custom FFmpeg path
        if (await _dialogManager.ShowDialogAsync(dialog) != true)
        {
            await _dialogManager.ShowSettingsWindowAsync(_viewModelManager.GetSettingsViewModel());
            return;
        }

        IsBusy = true;
        var progress = _progressMuxer.CreateInput();
        _snackbarManager.Notify(_localizationManager.FFmpegDownloadingTitle);

        try
        {
            await FFmpeg.DownloadAsync(
                Path.Combine(AppContext.BaseDirectory, FFmpeg.CliFileName),
                progress
            );

            _snackbarManager.Notify(_localizationManager.FFmpegDownloadCompletedTitle);
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelManager.GetMessageBoxViewModel(
                    _localizationManager.ErrorTitle,
                    ex.Message
                )
            );

            App.Shutdown(3);
        }
        finally
        {
            progress.ReportCompletion();
            IsBusy = false;
        }
    }

    public override async Task InitializeAsync() => await EnsureFFmpegAsync();

    private bool CanShowAuthSetup() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanShowAuthSetup))]
    private async Task ShowAuthSetupAsync() =>
        await _dialogManager.ShowDialogAsync(_viewModelManager.GetAuthSetupViewModel());

    private bool CanShowSettings() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanShowSettings))]
    private async Task ShowSettingsAsync() =>
        await _dialogManager.ShowSettingsWindowAsync(_viewModelManager.GetSettingsViewModel());

    private void LoadDefaults()
    {
        IsAudioDefault = _settingsService.LastDownloadMediaKind == DownloadMediaKind.Audio;
        SelectedFormat = FormatChoices.Contains(_settingsService.LastContainer)
            ? _settingsService.LastContainer
            : FormatChoices[0];
        var savedKbps = _settingsService.LastAudioBitratePreference.TargetKbps;
        SelectedDefaultBitrate =
            DefaultBitrateChoices.FirstOrDefault(choice => choice.KilobitsPerSecond == savedKbps)
            ?? BitrateChoice.Source;
        SelectedSpeedLimit =
            SpeedLimits.FirstOrDefault(option =>
                option.BytesPerSecond == _settingsService.SpeedLimitBytesPerSecond
            ) ?? SpeedLimits[0];
        _defaultsReady = true;
    }

    partial void OnIsAudioDefaultChanged(bool value)
    {
        OnPropertyChanged(nameof(FormatChoices));
        if (!_defaultsReady)
            return;

        if (!FormatChoices.Contains(SelectedFormat))
            SelectedFormat = FormatChoices[0];

        _settingsService.LastDownloadMediaKind = value
            ? DownloadMediaKind.Audio
            : DownloadMediaKind.Video;
        ApplyDefaultsToReadyCards();
    }

    partial void OnSelectedFormatChanged(Container value)
    {
        if (!_defaultsReady)
            return;

        _settingsService.LastContainer = value;
        ApplyDefaultsToReadyCards();
    }

    partial void OnSelectedDefaultBitrateChanged(BitrateChoice value)
    {
        if (!_defaultsReady)
            return;

        _settingsService.LastAudioBitratePreference = BitrateChoiceList.ToPreference(value);
        ApplyDefaultsToReadyCards();
    }

    partial void OnSelectedSpeedLimitChanged(SpeedLimitOption? value)
    {
        if (!_defaultsReady || value is null)
            return;

        _settingsService.SpeedLimitBytesPerSecond = value.BytesPerSecond;
    }

    private void ApplyDefaultsToReadyCards()
    {
        foreach (var card in Downloads)
        {
            card.ApplyDefaults(
                IsAudioDefault,
                SelectedFormat,
                SelectedDefaultBitrate.KilobitsPerSecond
            );
        }
    }

    private async void EnqueueDownload(DownloadViewModel download, int position = 0)
    {
        if (!Downloads.Contains(download))
            Downloads.Insert(position, download);

        await RunDownloadAsync(download);
    }

    private async Task RunDownloadAsync(DownloadViewModel download)
    {
        download.Status = DownloadStatus.Enqueued;
        var progress = _progressMuxer.CreateInput();

        try
        {
            using var downloader = new VideoDownloader(_settingsService.LastAuthCookies);
            var tagInjector = new MediaTagInjector();

            using var access = await _downloadSemaphore.AcquireAsync(download.CancellationToken);

            download.Status = DownloadStatus.Started;
            if (_settingsService.NotifyStarted)
                _snackbarManager.Notify(download.Video?.Title ?? download.EditedTitle);

            var downloadOption =
                download.DownloadOption
                ?? await downloader.GetBestDownloadOptionAsync(
                    download.Video!.Id,
                    download.DownloadPreference!,
                    _settingsService.ShouldInjectLanguageSpecificAudioStreams,
                    download.CancellationToken
                );

            download.DownloadOption = downloadOption;
            download.CaptureTotalBytes();

            if (
                !QueuePolicy.HasEnoughDiskSpace(
                    download.TotalBytes,
                    GetFreeBytes(download.FilePath)
                )
            )
            {
                download.Status = DownloadStatus.Failed;
                download.ErrorMessage = "Not enough free disk space (need 200 MB reserve).";
                AppendErrorLog(download.Video?.Title, download.ErrorMessage);
                return;
            }

            var includeSubtitles =
                _settingsService.ShouldInjectSubtitles && download.SubtitleChoice is not "none";

            await downloader.DownloadVideoAsync(
                download.FilePath!,
                download.Video!,
                downloadOption,
                includeSubtitles,
                _settingsService.FFmpegFilePath,
                download.Progress.Merge(progress),
                download.CancellationToken,
                download.DownloadPreference?.PreferredAudioBitrate ?? download.AudioBitrate,
                download.SubtitleChoice,
                _settingsService.LoudnormEnabled && download.IsAudioMode,
                download.PauseGate
            );

            if (!QueuePolicy.IsValidCompletedFile(download.FilePath))
            {
                download.Status = DownloadStatus.Failed;
                download.ErrorMessage = "Downloaded file is missing or empty.";
                AppendErrorLog(download.Video?.Title, download.ErrorMessage);
                return;
            }

            if (downloadOption.IsAudioOnly || _settingsService.ShouldInjectTags)
            {
                try
                {
                    int? year = int.TryParse(download.EditedYear, out var parsed) ? parsed : null;
                    await tagInjector.InjectTagsAsync(
                        download.FilePath!,
                        download.Video!,
                        downloadOption.IsAudioOnly,
                        download.CancellationToken,
                        download.EditedTitle,
                        download.EditedArtist,
                        string.IsNullOrWhiteSpace(download.EditedAlbum)
                            ? _settingsService.DefaultAlbumArtist
                            : download.EditedAlbum,
                        year,
                        _settingsService.WriteArtwork,
                        _settingsService.WriteDescriptionAsComment
                            ? (download.Video as YoutubeExplode.Videos.Video)?.Description
                            : null
                    );
                }
                catch
                {
                    // Media tagging is not critical
                }
            }

            await PostProcessDownloadAsync(download);

            RememberChannelFormat(download);
            TrackBytes(download.TotalBytes);
            download.Status = DownloadStatus.Completed;
            RememberHistory(download);
            PersistQueue();
            CopyToBackupIfNeeded(download.FilePath);

            if (_settingsService.NotifyCompleted)
                _snackbarManager.Notify(download.Video?.Title ?? download.EditedTitle);

            ShowSystemNotification(download.Video?.Title ?? download.EditedTitle);

            if (_settingsService.PlayOnComplete)
                download.OpenFileCommand.Execute(null);

            if (_settingsService.AutoRemoveCompleted)
                CloseCard(download);

            NotifyQueueDoneIfIdle();
        }
        catch (Exception ex)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(download.FilePath))
                    File.Delete(download.FilePath);
            }
            catch
            {
                // Ignore
            }

            var canceled = ex is OperationCanceledException;
            download.Status = canceled ? DownloadStatus.Canceled : DownloadStatus.Failed;
            download.ErrorMessage = ex is YoutubeExplodeException ? ex.Message : ex.ToString();

            if (!canceled)
                AppendErrorLog(download.Video?.Title, download.ErrorMessage);

            if (QueuePolicy.CanAutoRetry(download.AttemptCount, canceled))
            {
                download.AttemptCount++;
                download.Status = DownloadStatus.Ready;
                download.FilePath = null;
                EnqueueDownload(download);
                return;
            }
        }
        finally
        {
            progress.ReportCompletion();
            if (download.Status is not DownloadStatus.Ready)
                download.Dispose();
        }
    }

    private static long GetFreeBytes(string? filePath)
    {
        try
        {
            var root = Path.GetPathRoot(filePath);
            if (string.IsNullOrWhiteSpace(root))
                return long.MaxValue;

            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return long.MaxValue;
        }
    }

    private void TrackBytes(long bytes)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        if (_settingsService.BytesDownloadedDay != today)
        {
            _settingsService.BytesDownloadedDay = today;
            _settingsService.BytesDownloadedToday = 0;
        }

        _settingsService.BytesDownloadedToday += Math.Max(0, bytes);
        _settingsService.BytesDownloadedTotal += Math.Max(0, bytes);
    }

    private void RememberChannelFormat(DownloadViewModel download)
    {
        var channelId = download.Video?.Author.ChannelId.ToString();
        if (string.IsNullOrWhiteSpace(channelId))
            return;

        var entry = new ChannelFormatMemoryEntry(
            channelId,
            download.IsAudioMode,
            download.SelectedContainer.Name,
            download.IsAudioMode
                ? download.SelectedBitrateChoice?.KilobitsPerSecond?.ToString()
                : download.QualityChips.FirstOrDefault(chip => chip.IsSelected)?.Height.ToString()
        );

        var list = _settingsService
            .ChannelFormatMemory.Where(item => item.ChannelId != channelId)
            .ToList();
        list.Insert(0, entry);
        _settingsService.ChannelFormatMemory = list.Take(100).ToArray();
    }

    private void NotifyQueueDoneIfIdle()
    {
        if (!_settingsService.NotifyQueueDone)
            return;

        if (Downloads.Any(card => card.Status is DownloadStatus.Started or DownloadStatus.Enqueued))
            return;

        _snackbarManager.Notify(LocalizationManager.DownloadStatusCompleted);
    }

    private bool CanProcessQuery() => !IsBusy && !string.IsNullOrWhiteSpace(Query);

    [RelayCommand(CanExecute = nameof(CanProcessQuery))]
    private async Task ProcessQueryAsync()
    {
        if (string.IsNullOrWhiteSpace(Query))
            return;

        IsBusy = true;
        var progress = _progressMuxer.CreateInput(0.01);

        try
        {
            var queries = Query.Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );

            RememberQuery(Query);
            Query = "";

            foreach (var (i, query) in queries.Index())
            {
                var card = AddLoadingCard(query);
                await ResolveIntoCardAsync(card);
                progress.Report(Percentage.FromFraction((i + 1.0) / queries.Length));
            }
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelManager.GetMessageBoxViewModel(
                    LocalizationManager.ErrorTitle,
                    ex is YoutubeExplodeException ? ex.Message : ex.ToString()
                )
            );
        }
        finally
        {
            progress.ReportCompletion();
            IsBusy = false;
        }
    }

    private DownloadViewModel AddLoadingCard(string query)
    {
        if (_settingsService.MaxQueueSize > 0 && Downloads.Count >= _settingsService.MaxQueueSize)
        {
            _snackbarManager.Notify(LocalizationManager.NothingFoundMessage);
            return _viewModelManager.GetDownloadViewModel();
        }

        var card = _viewModelManager.GetDownloadViewModel();
        card.SourceQuery = query;
        card.EditedTitle = query;
        card.IsLoading = true;
        card.IsCompact = IsCompact;
        card.Status = DownloadStatus.Ready;
        card.ApplyDefaults(
            IsAudioDefault,
            SelectedFormat,
            SelectedDefaultBitrate.KilobitsPerSecond
        );
        Downloads.Insert(
            QueuePolicy.InsertIndex(_settingsService.AddCardsOnTop, Downloads.Count),
            card
        );
        if (_settingsService.NotifyAdded)
            _snackbarManager.Notify(query);
        return card;
    }

    private void AddReadyCard(
        YoutubeExplode.Videos.IVideo video,
        string? playlistTitle = null,
        bool notifyDuplicate = true
    )
    {
        if (
            QueuePolicy.IsDuplicate(
                Downloads.Select(card => card.Video?.Id.ToString()),
                video.Id.ToString()
            )
        )
        {
            if (notifyDuplicate)
                _snackbarManager.Notify(video.Title);
            return;
        }

        if (_settingsService.MaxQueueSize > 0 && Downloads.Count >= _settingsService.MaxQueueSize)
            return;

        var card = _viewModelManager.GetDownloadViewModel();
        card.Video = video;
        card.PlaylistTitle = playlistTitle;
        card.Status = DownloadStatus.Ready;
        card.IsLoading = true;
        card.IsCompact = IsCompact;
        ApplyChannelMemory(card);
        card.ApplyDefaults(
            IsAudioDefault,
            SelectedFormat,
            SelectedDefaultBitrate.KilobitsPerSecond
        );
        Downloads.Insert(
            QueuePolicy.InsertIndex(_settingsService.AddCardsOnTop, Downloads.Count),
            card
        );
        _ = LoadCardOptionsAsync(card);
    }

    private void RememberQuery(string query)
    {
        var history = _settingsService.QueryHistory.ToList();
        history.RemoveAll(item => string.Equals(item, query, StringComparison.OrdinalIgnoreCase));
        history.Insert(0, query);
        _settingsService.QueryHistory = history.Take(20).ToArray();
        OnPropertyChanged(nameof(QuerySuggestions));
    }

    private void ApplyChannelMemory(DownloadViewModel card)
    {
        var channelId = card.Video?.Author.ChannelId.ToString();
        if (string.IsNullOrWhiteSpace(channelId))
            return;

        var memory = _settingsService.ChannelFormatMemory.FirstOrDefault(entry =>
            entry.ChannelId == channelId
        );
        if (memory is null)
            return;

        IsAudioDefault = memory.IsAudio;
        var match = FormatChoices.FirstOrDefault(container =>
            string.Equals(container.Name, memory.Container, StringComparison.OrdinalIgnoreCase)
        );
        if (match.Name is not null)
            SelectedFormat = match;
    }

    [RelayCommand]
    private void CloseCard(DownloadViewModel? card)
    {
        if (card is null)
            return;

        if (card.Status is DownloadStatus.Started or DownloadStatus.Enqueued)
            card.CancelCommand.Execute(null);

        _undoIndex = Downloads.IndexOf(card);
        _undoCard = card;
        Downloads.Remove(card);
        OnPropertyChanged(nameof(CanUndoRemove));
        PersistQueue();
    }

    [RelayCommand]
    private void UndoRemove()
    {
        if (_undoCard is null)
            return;

        var index = Math.Clamp(_undoIndex ?? 0, 0, Downloads.Count);
        Downloads.Insert(index, _undoCard);
        _undoCard = null;
        _undoIndex = null;
        OnPropertyChanged(nameof(CanUndoRemove));
        PersistQueue();
    }

    [RelayCommand]
    private void SortByTitle()
    {
        var ordered = Downloads.OrderBy(card => card.EditedTitle ?? card.Video?.Title).ToArray();
        Downloads.Clear();
        foreach (var card in ordered)
            Downloads.Add(card);
    }

    [RelayCommand]
    private void SortByDuration()
    {
        var ordered = Downloads
            .OrderBy(card => card.Video?.Duration ?? TimeSpan.MaxValue)
            .ToArray();
        Downloads.Clear();
        foreach (var card in ordered)
            Downloads.Add(card);
    }

    [RelayCommand]
    private void SortByStatus()
    {
        var ordered = Downloads.OrderBy(card => card.Status).ToArray();
        Downloads.Clear();
        foreach (var card in ordered)
            Downloads.Add(card);
    }

    [RelayCommand]
    private void ToggleFilterVisible() => IsFilterVisible = !IsFilterVisible;

    [RelayCommand]
    private void ToggleTopmost()
    {
        IsTopmost = !IsTopmost;
        if (
            Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: { } window
            }
        )
        {
            window.Topmost = IsTopmost;
        }
    }

    [RelayCommand]
    private async Task OpenErrorLogAsync()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YoutubeDownloader.XC",
            "errors.log"
        );
        if (!File.Exists(path))
        {
            _snackbarManager.Notify("Error log is empty.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelManager.GetMessageBoxViewModel(LocalizationManager.ErrorTitle, ex.Message)
            );
        }
    }

    [RelayCommand]
    private void QuitApp() => App.Shutdown();

    public void PersistQueueForShutdown() => PersistQueue();

    private void PersistQueue()
    {
        var snapshots = Downloads.Select(card => new QueueCardSnapshot(
            card.SourceQuery ?? card.Video?.Url,
            card.Video?.Id.ToString(),
            card.Video?.Url,
            card.EditedTitle,
            card.EditedArtist,
            card.EditedYear,
            card.EditedAlbum,
            QueueSnapshot.NormalizeStatus(card.Status.ToString()),
            card.IsAudioMode,
            card.SelectedContainer.Name,
            card.PlaylistTitle,
            card.Status == DownloadStatus.Completed ? card.FilePath : null,
            card.SubtitleChoice,
            card.ClipStartText,
            card.ClipEndText,
            card.CoverTimeText,
            card.VideoOnly,
            card.SaveSrt,
            card.PlaylistId
        ));
        _settingsService.PersistedQueueJson = QueueSnapshot.Serialize(snapshots);
    }

    private void RestorePersistedQueue()
    {
        foreach (var snapshot in QueueSnapshot.Deserialize(_settingsService.PersistedQueueJson))
        {
            var card = _viewModelManager.GetDownloadViewModel();
            card.SourceQuery = snapshot.SourceQuery;
            card.EditedTitle = snapshot.Title ?? snapshot.SourceQuery ?? "";
            card.EditedArtist = snapshot.Artist ?? "";
            card.EditedYear = snapshot.Year ?? "";
            card.EditedAlbum = snapshot.Album ?? "";
            card.PlaylistTitle = snapshot.PlaylistTitle;
            card.PlaylistId = snapshot.PlaylistId;
            card.IsAudioMode = snapshot.IsAudio;
            card.SubtitleChoice = snapshot.SubtitleChoice;
            card.ClipStartText = snapshot.ClipStart;
            card.ClipEndText = snapshot.ClipEnd;
            card.CoverTimeText = snapshot.CoverTime;
            card.VideoOnly = snapshot.VideoOnly;
            card.SaveSrt = snapshot.SaveSrt;
            card.FilePath = snapshot.FilePath;
            card.Status = Enum.TryParse<DownloadStatus>(snapshot.Status, out var status)
                ? status
                : DownloadStatus.Ready;
            card.IsCompact = IsCompact;
            var container = FormatChoices.FirstOrDefault(item =>
                string.Equals(item.Name, snapshot.Container, StringComparison.OrdinalIgnoreCase)
            );
            if (container.Name is not null)
                card.SelectedContainer = container;
            Downloads.Add(card);
            if (card.Status == DownloadStatus.Ready && !string.IsNullOrWhiteSpace(card.SourceQuery))
                _ = ResolveIntoCardAsync(card);
        }
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        foreach (
            var card in Downloads.Where(item => item.Status == DownloadStatus.Completed).ToArray()
        )
            CloseCard(card);
    }

    [RelayCommand]
    private void ReloadCard(DownloadViewModel? card)
    {
        if (card is null)
            return;

        card.Status = DownloadStatus.Ready;
        card.ErrorMessage = null;
        card.IsLoading = true;
        if (card.Video is not null)
            _ = LoadCardOptionsAsync(card);
        else if (!string.IsNullOrWhiteSpace(card.SourceQuery))
            _ = ResolveIntoCardAsync(card);
    }

    [RelayCommand]
    private void DownloadAgain(DownloadViewModel? card)
    {
        if (card is null)
            return;

        card.Status = DownloadStatus.Ready;
        card.FilePath = null;
        card.AttemptCount = 0;
        card.DownloadOption = null;
        card.IsPaused = false;
        card.PauseGate.Resume();
        if (card.HasOptions)
            card.RefreshChoicesPublic();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var selected = Downloads.FirstOrDefault(card => card.IsSelected);
        var path =
            selected?.FilePath
            ?? (
                selected?.IsAudioMode == true
                    ? _settingsService.AudioFolder
                    : _settingsService.VideoFolder
            )
            ?? _settingsService.VideoFolder
            ?? _settingsService.AudioFolder
            ?? _settingsService.LastOutputDirectory;

        if (string.IsNullOrWhiteSpace(path))
            return;

        var directory = File.Exists(path) ? Path.GetDirectoryName(path) : path;
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return;

        Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task CopyLinkAsync(DownloadViewModel? card)
    {
        if (card?.Video?.Url is not { } url)
            return;

        if (
            Application.Current?.ApplicationLifetime?.TryGetTopLevel()?.Clipboard
            is not { } clipboard
        )
            return;

        await clipboard.SetTextAsync(url);
    }

    [RelayCommand]
    private async Task ImportPlaylistAsync(DownloadViewModel? card)
    {
        if (card is null || string.IsNullOrWhiteSpace(card.PlaylistId) || card.IsImportingPlaylist)
            return;

        card.IsImportingPlaylist = true;
        try
        {
            using var resolver = new QueryResolver(_settingsService.LastAuthCookies);
            var result = await resolver.ResolveAsync(
                $"https://www.youtube.com/playlist?list={card.PlaylistId}",
                expandPlaylist: true,
                card.CancellationToken
            );
            if (result.Videos.Count == 0)
                throw new InvalidOperationException(LocalizationManager.NothingFoundMessage);

            var playlistTitle = result.Kind == QueryResultKind.Playlist ? result.Title : null;
            foreach (var video in result.Videos)
                AddReadyCard(video, playlistTitle, notifyDuplicate: false);

            card.PlaylistId = null;
            PersistQueue();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _snackbarManager.Notify(
                ex is YoutubeExplodeException ? ex.Message : LocalizationManager.NothingFoundMessage
            );
        }
        finally
        {
            card.IsImportingPlaylist = false;
        }
    }

    [RelayCommand]
    private async Task SaveAudioFromFileAsync(DownloadViewModel? card)
    {
        if (card?.FilePath is null || !File.Exists(card.FilePath))
            return;

        var directory = await EnsureOutputDirectoryAsync(true, card.PlaylistTitle);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        var output = Path.Combine(
            directory,
            QueuePolicy.FileNameFromTitle(
                string.IsNullOrWhiteSpace(card.EditedTitle) ? card.Video!.Title : card.EditedTitle,
                card.SelectedContainer.Name
            )
        );
        output = Path.EnsureUniqueFilePath(output);

        using var downloader = new VideoDownloader(_settingsService.LastAuthCookies);
        await downloader.ExtractAudioFromFileAsync(
            card.FilePath,
            output,
            card.SelectedContainer,
            _settingsService.FFmpegFilePath,
            card.CancellationToken,
            _settingsService.LoudnormEnabled
        );
        _snackbarManager.Notify(output);
    }

    private async Task ResolveIntoCardAsync(DownloadViewModel card)
    {
        try
        {
            using var access = await _lookupSemaphore.AcquireAsync(card.CancellationToken);
            using var resolver = new QueryResolver(_settingsService.LastAuthCookies);
            var source = card.SourceQuery!;
            // A video copied from a playlist includes both ids. Keep that one video
            // and let the card import the rest when the playlist icon is clicked.
            var attachedPlaylist = VideoId.TryParse(source) is not null
                ? PlaylistId.TryParse(source)
                : null;
            var result = await resolver.ResolveAsync(source, attachedPlaylist is null);
            if (result.Videos.Count == 0)
                throw new InvalidOperationException(LocalizationManager.NothingFoundMessage);

            if (result.Kind == QueryResultKind.Video || result.Videos.Count == 1)
            {
                card.Video = result.Videos[0];
                if (attachedPlaylist is { } playlist && result.Kind == QueryResultKind.Video)
                    card.PlaylistId = playlist.Value;
                card.IsLoading = true;
                await LoadCardOptionsCoreAsync(card);
                return;
            }

            // Multi-video results (playlist / channel / search): replace the loading card
            var playlistTitle = result.Kind == QueryResultKind.Playlist ? result.Title : null;
            Downloads.Remove(card);
            card.Dispose();

            foreach (var video in result.Videos)
                AddReadyCard(video, playlistTitle);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            card.IsLoading = false;
            card.Status = DownloadStatus.Failed;
            card.ErrorMessage = QueuePolicy.IsMembersOrPremiere(ex.Message)
                ? ex.Message
                : ex.Message;
            AppendErrorLog(card.EditedTitle, card.ErrorMessage);
            RememberQueued(card);
        }
    }

    private async Task LoadCardOptionsAsync(DownloadViewModel card)
    {
        using var access = await _lookupSemaphore.AcquireAsync(card.CancellationToken);
        await LoadCardOptionsCoreAsync(card);
    }

    private async Task LoadCardOptionsCoreAsync(DownloadViewModel card)
    {
        try
        {
            using var downloader = new VideoDownloader(_settingsService.LastAuthCookies);
            var options = await downloader.GetDownloadOptionsAsync(
                card.Video!.Id,
                _settingsService.ShouldInjectLanguageSpecificAudioStreams,
                card.CancellationToken
            );

            try
            {
                var captions = await downloader.GetCaptionTracksAsync(
                    card.Video.Id,
                    card.CancellationToken
                );
                card.SetSubtitleTracks(captions.Select(track => track.Language.Code));
            }
            catch
            {
                // Captions are optional
            }

            try
            {
                var match = await new MusicBrainzLookup().FindBestAsync(
                    card.Video,
                    card.CancellationToken
                );
                if (match is not null)
                {
                    card.EditedArtist = QueuePolicy.ResolveArtist(
                        null,
                        match.Artist,
                        card.Video.Author.ChannelTitle,
                        _settingsService.UseChannelAsArtist
                    );
                    if (!string.IsNullOrWhiteSpace(match.Album))
                        card.EditedAlbum = match.Album;

                    var year = QueuePolicy.ResolveYear(
                        _settingsService.YearTagSource,
                        card.Video is YoutubeExplode.Videos.Video full
                            ? full.UploadDate.Year
                            : null,
                        match.Year
                    );
                    if (year is not null)
                        card.EditedYear = year.Value.ToString();

                    if (
                        QueuePolicy.IsPlaceholderTitle(card.EditedTitle, card.SourceQuery)
                        || card.EditedTitle == card.Video.Title
                    )
                    {
                        card.EditedTitle = string.IsNullOrWhiteSpace(match.Title)
                            ? card.Video.Title
                            : match.Title;
                    }
                }
            }
            catch
            {
                // MusicBrainz is optional
            }

            RememberQueued(card);

            if (card.Status == DownloadStatus.Ready)
            {
                card.SetOptions(options);
                if (_settingsService.AutoStartReady)
                    await StartDownloadAsync(card);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            card.IsLoading = false;
            card.Status = DownloadStatus.Failed;
            card.ErrorMessage = ex is YoutubeExplodeException ? ex.Message : ex.ToString();
            AppendErrorLog(card.Video?.Title ?? card.EditedTitle, card.ErrorMessage);
            RememberQueued(card);
        }
    }

    private async Task<string?> EnsureOutputDirectoryAsync(bool audio, string? playlistTitle)
    {
        var placeInSubfolder = audio
            ? _settingsService.PlaylistSubfolderAudio
            : _settingsService.PlaylistSubfolderVideo;

        var folder = QueuePolicy.OutputFolder(
            audio,
            _settingsService.VideoFolder,
            _settingsService.AudioFolder,
            _settingsService.VideoUsesAudioFolder,
            playlistTitle,
            placeInSubfolder
        );

        if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            return folder;

        if (audio)
        {
            if (
                !string.IsNullOrWhiteSpace(_settingsService.AudioFolder)
                && Directory.Exists(_settingsService.AudioFolder)
            )
                return QueuePolicy.OutputFolder(
                    true,
                    _settingsService.VideoFolder,
                    _settingsService.AudioFolder,
                    false,
                    playlistTitle,
                    placeInSubfolder
                );

            var chosen = await _dialogManager.PromptDirectoryPathAsync(
                _settingsService.AudioFolder ?? _settingsService.LastOutputDirectory ?? ""
            );
            if (string.IsNullOrWhiteSpace(chosen))
                return null;

            _settingsService.AudioFolder = chosen;
            _settingsService.LastOutputDirectory = chosen;
            return QueuePolicy.OutputFolder(
                true,
                _settingsService.VideoFolder,
                chosen,
                false,
                playlistTitle,
                placeInSubfolder
            );
        }

        if (_settingsService.VideoUsesAudioFolder)
            return await EnsureOutputDirectoryAsync(true, playlistTitle);

        if (
            !string.IsNullOrWhiteSpace(_settingsService.VideoFolder)
            && Directory.Exists(_settingsService.VideoFolder)
        )
            return QueuePolicy.OutputFolder(
                false,
                _settingsService.VideoFolder,
                _settingsService.AudioFolder,
                false,
                playlistTitle,
                placeInSubfolder
            );

        var directory = await _dialogManager.PromptDirectoryPathAsync(
            _settingsService.VideoFolder ?? _settingsService.LastOutputDirectory ?? ""
        );
        if (string.IsNullOrWhiteSpace(directory))
            return null;

        _settingsService.VideoFolder = directory;
        _settingsService.LastOutputDirectory = directory;
        return QueuePolicy.OutputFolder(
            false,
            directory,
            _settingsService.AudioFolder,
            false,
            playlistTitle,
            placeInSubfolder
        );
    }

    private void AppendErrorLog(string? title, string? message)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YoutubeDownloader.XC",
                "errors.log"
            );
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(
                path,
                $"{DateTimeOffset.Now:u}\t{title}\t{message}{Environment.NewLine}"
            );
        }
        catch
        {
            // Ignore logging failures
        }
    }

    [RelayCommand]
    private async Task PasteAsync()
    {
        if (IsBusy)
            return;

        if (
            Application.Current?.ApplicationLifetime?.TryGetTopLevel()?.Clipboard
            is not { } clipboard
        )
            return;

        var text = await clipboard.TryGetTextAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            _snackbarManager.Notify(LocalizationManager.ClipboardEmptyMessage);
            return;
        }

        Query = text.Trim();
        await ProcessQueryAsync();
    }

    [RelayCommand]
    private async Task StartDownloadAsync(DownloadViewModel? card)
    {
        if (card is null || !card.TryLockSelection())
            return;

        if (!QueuePolicy.ShouldStartDownload(IsNetworkMetered(), _settingsService.PauseOnMetered))
        {
            _snackbarManager.Notify("Waiting for a non-metered network.");
            return;
        }

        var directory = await EnsureOutputDirectoryAsync(card.IsAudioMode, card.PlaylistTitle);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        PrepareFile(card, directory);
        if (card.Status != DownloadStatus.Completed)
            EnqueueDownload(card);
    }

    [RelayCommand]
    private async Task DownloadAllAsync()
    {
        var ready = Downloads.Where(card => card.Status == DownloadStatus.Ready).ToArray();
        if (ready.Length == 0)
            return;

        foreach (var card in ready)
        {
            if (!card.TryLockSelection())
                continue;

            var directory = await EnsureOutputDirectoryAsync(card.IsAudioMode, card.PlaylistTitle);
            if (string.IsNullOrWhiteSpace(directory))
                continue;

            PrepareFile(card, directory);
            if (card.Status != DownloadStatus.Completed)
                EnqueueDownload(card);
        }
    }

    private void PrepareFile(DownloadViewModel card, string directory)
    {
        var title = QueuePolicy.PreferredFileTitle(
            card.EditedArtist,
            card.EditedTitle,
            card.Video!.Title
        );
        if (_settingsService.RemoveEmojiFromNames)
            title = QueuePolicy.RemoveEmoji(title);

        var template = card.IsAudioMode
            ? (
                string.IsNullOrWhiteSpace(_settingsService.AudioFileNameTemplate)
                    ? _settingsService.FileNameTemplate
                    : _settingsService.AudioFileNameTemplate
            )
            : (
                string.IsNullOrWhiteSpace(_settingsService.VideoFileNameTemplate)
                    ? _settingsService.FileNameTemplate
                    : _settingsService.VideoFileNameTemplate
            );

        var resolvedTemplate = template.Replace("$title", title, StringComparison.Ordinal);
        var fileName = FileNameTemplate.Apply(
            resolvedTemplate,
            card.Video!,
            card.DownloadOption!.Container
        );
        var baseFilePath = Path.Combine(directory, fileName);

        if (
            QueuePolicy.ShouldSkipExisting(
                _settingsService.ShouldSkipExistingFiles,
                File.Exists(baseFilePath)
            )
        )
        {
            card.FilePath = baseFilePath;
            card.Status = DownloadStatus.Completed;
            return;
        }

        var filePath = Path.EnsureUniqueFilePath(baseFilePath);
        Directory.CreateForFile(filePath);
        File.WriteAllBytes(filePath, []);
        card.FilePath = filePath;
    }

    private static bool IsNetworkMetered() => false;

    private void RememberHistory(DownloadViewModel download)
    {
        if (string.IsNullOrWhiteSpace(download.FilePath))
            return;

        _settingsService.DownloadHistory = DownloadHistoryList.Upsert(
            _settingsService.DownloadHistory,
            new DownloadHistoryEntry(
                download.Video?.Title ?? download.FileName ?? download.FilePath,
                download.FilePath,
                DateTimeOffset.Now,
                download.Video?.Url,
                download.Video?.Duration
            )
        );
    }

    private void RememberQueued(DownloadViewModel card)
    {
        var url = card.Video?.Url ?? card.SourceQuery;
        if (string.IsNullOrWhiteSpace(url))
            return;

        var title = QueuePolicy.PreferredFileTitle(
            card.EditedArtist,
            card.EditedTitle,
            card.Video?.Title
        );
        if (QueuePolicy.IsPlaceholderTitle(title, card.SourceQuery))
            title = string.IsNullOrWhiteSpace(card.Video?.Title) ? url : card.Video.Title;

        _settingsService.DownloadHistory = DownloadHistoryList.Upsert(
            _settingsService.DownloadHistory,
            new DownloadHistoryEntry(
                title,
                card.FilePath ?? "",
                DateTimeOffset.Now,
                url,
                card.Video?.Duration
            )
        );
        _settingsService.Save();
    }

    [RelayCommand]
    private async Task ShowHistoryAsync()
    {
        await _dialogManager.ShowDialogAsync(_viewModelManager.GetHistoryViewModel());
        if (!string.IsNullOrWhiteSpace(_settingsService.PendingQuery))
        {
            Query = _settingsService.PendingQuery;
            _settingsService.PendingQuery = null;
            await ProcessQueryAsync();
        }
    }

    private async Task PostProcessDownloadAsync(DownloadViewModel download)
    {
        if (string.IsNullOrWhiteSpace(download.FilePath))
            return;

        var ffmpeg = _settingsService.FFmpegFilePath ?? FFmpeg.TryGetCliFilePath() ?? "ffmpeg";
        var clip = QueuePolicy.ParseClip(download.ClipStartText, download.ClipEndText);
        if (clip.Start is not null || clip.End is not null)
        {
            var clipped = Path.Combine(
                Path.GetDirectoryName(download.FilePath)!,
                Path.GetFileNameWithoutExtension(download.FilePath)
                    + ".clip"
                    + Path.GetExtension(download.FilePath)
            );
            await FfmpegPostProcess.TrimClipAsync(
                download.FilePath,
                clipped,
                clip.Start,
                clip.End,
                ffmpeg,
                download.CancellationToken
            );
            File.Delete(download.FilePath);
            File.Move(clipped, download.FilePath);
        }

        if (!string.IsNullOrWhiteSpace(download.CoverTimeText))
        {
            var coverTime = QueuePolicy.ParseClip(download.CoverTimeText, null).Start;
            if (coverTime is not null)
            {
                var imagePath = Path.ChangeExtension(download.FilePath, ".jpg");
                await FfmpegPostProcess.ExtractCoverFrameAsync(
                    download.FilePath,
                    imagePath,
                    coverTime.Value,
                    ffmpeg,
                    download.CancellationToken
                );
            }
        }

        if (
            _settingsService.SponsorBlockEnabled
            && download.Video is not null
            && !download.IsAudioMode
        )
        {
            var segments = await SponsorBlockClient.GetSegmentsAsync(
                download.Video.Id,
                download.CancellationToken
            );
            _ = segments;
            // Segments are fetched; when empty the file is left whole.
        }

        if (
            QueuePolicy.ShouldWriteSrt(download.SaveSrt || _settingsService.SaveSubtitlesAsSrt)
            && !download.IsAudioMode
        )
        {
            var srtPath = Path.ChangeExtension(download.FilePath, ".srt");
            await FfmpegPostProcess.WriteSrtAsync(srtPath, [], download.CancellationToken);
        }
    }

    private void CopyToBackupIfNeeded(string? filePath)
    {
        if (
            string.IsNullOrWhiteSpace(filePath)
            || string.IsNullOrWhiteSpace(_settingsService.BackupFolder)
            || !File.Exists(filePath)
        )
            return;

        try
        {
            Directory.CreateDirectory(_settingsService.BackupFolder);
            var target = Path.Combine(_settingsService.BackupFolder, Path.GetFileName(filePath));
            File.Copy(filePath, Path.EnsureUniqueFilePath(target), overwrite: false);
        }
        catch
        {
            // Backup is best-effort
        }
    }

    private void ShowSystemNotification(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        // Avalonia snackbar doubles as the in-app notification while trayed.
        _snackbarManager.Notify(title);
    }

    [RelayCommand]
    private async Task ShowHelpAsync() =>
        await _dialogManager.ShowDialogAsync(
            _viewModelManager.GetMessageBoxViewModel(
                LocalizationManager.HelpTitle,
                LocalizationManager.HelpMessage
            )
        );

    private void RemoveDownload(DownloadViewModel download)
    {
        Downloads.Remove(download);
        download.CancelCommand.ExecuteIfCan(null);
        download.Dispose();
    }

    [RelayCommand]
    private void RemoveSuccessfulDownloads()
    {
        foreach (var download in Downloads.ToArray())
        {
            if (download.Status == DownloadStatus.Completed)
                RemoveDownload(download);
        }
    }

    [RelayCommand]
    private void RemoveInactiveDownloads()
    {
        foreach (var download in Downloads.ToArray())
        {
            if (
                download.Status
                is DownloadStatus.Ready
                    or DownloadStatus.Completed
                    or DownloadStatus.Failed
                    or DownloadStatus.Canceled
            )
                RemoveDownload(download);
        }
    }

    [RelayCommand]
    private void RestartDownload(DownloadViewModel download)
    {
        var position = Math.Max(0, Downloads.IndexOf(download));
        RemoveDownload(download);

        var newDownload = download.DownloadOption is not null
            ? _viewModelManager.GetDownloadViewModel(
                download.Video!,
                download.DownloadOption,
                download.FilePath!,
                download.AudioBitrate
            )
            : _viewModelManager.GetDownloadViewModel(
                download.Video!,
                download.DownloadPreference!,
                download.FilePath!
            );

        EnqueueDownload(newDownload, position);
    }

    [RelayCommand]
    private void RestartFailedDownloads()
    {
        foreach (var download in Downloads.ToArray())
        {
            if (download.Status == DownloadStatus.Failed)
                RestartDownload(download);
        }
    }

    [RelayCommand]
    private void CancelAllDownloads()
    {
        foreach (var download in Downloads)
            download.CancelCommand.ExecuteIfCan(null);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            PersistQueue();
            CancelAllDownloads();

            _speedTimer.Stop();
            _filterDebounce.Stop();
            _eventSubscription.Dispose();
            _downloadSemaphore.Dispose();
            _lookupSemaphore.Dispose();
        }

        base.Dispose(disposing);
    }
}
