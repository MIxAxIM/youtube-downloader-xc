using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeDownloader.XC.Core.Downloading;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.XC.ViewModels.Components;

public partial class DownloadViewModel
{
    private IReadOnlyList<VideoDownloadOption> _options = [];
    private int? _selectedHeight;
    private int? _selectedBitrateKbps;
    private long _lastSampledBytes;
    private DateTimeOffset _lastSampledAt;
    private bool _refreshingChoices;

    public IReadOnlyList<Container> FormatChoices => MediaFormats.Containers(IsAudioMode);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowQualityChips))]
    [NotifyPropertyChangedFor(nameof(ShowAudioDetails))]
    [NotifyPropertyChangedFor(nameof(FormatChoices))]
    [NotifyCanExecuteChangedFor(nameof(SelectQualityCommand))]
    public partial bool IsAudioMode { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectQualityCommand))]
    public partial Container SelectedContainer { get; set; } = Container.Mp4;

    [ObservableProperty]
    public partial IReadOnlyList<QualityChip> QualityChips { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<BitrateChoice> BitrateChoices { get; set; } = [];

    [ObservableProperty]
    public partial BitrateChoice? SelectedBitrateChoice { get; set; }

    [ObservableProperty]
    public partial string ProgressLabel { get; set; } = "";

    [ObservableProperty]
    public partial double BytesPerSecond { get; set; }

    public long TotalBytes { get; private set; }

    public bool CanEdit => Status == DownloadStatus.Ready;

    public bool HasOptions => _options.Count > 0;

    public bool ShowQualityChips => CanEdit && !IsAudioMode;

    public bool ShowAudioDetails => CanEdit && !IsCompact;

    public bool ShowSubtitleChoice => CanEdit && !IsAudioMode && !IsCompact;

    public bool CanStart => CanEdit && HasOptions && HasMatchingSelection;

    public bool CanPause => Status == DownloadStatus.Started && !IsPaused;

    public bool CanResume => Status == DownloadStatus.Started && IsPaused;

    public double FillFraction =>
        QueuePolicy.ProgressFill(
            Status == DownloadStatus.Completed,
            Status == DownloadStatus.Started,
            Progress.Current.Fraction
        );

    public string StatusAccentBrushKey =>
        Status switch
        {
            DownloadStatus.Started when IsPaused => "AppStatusPausedBrush",
            DownloadStatus.Started or DownloadStatus.Enqueued => "AppStatusDownloadingBrush",
            DownloadStatus.Completed => "AppStatusCompletedBrush",
            DownloadStatus.Failed => "AppStatusFailedBrush",
            DownloadStatus.Canceled => "AppStatusPausedBrush",
            _ => "AppStatusReadyBrush",
        };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileNamePreview))]
    public partial string EditedTitle { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileNamePreview))]
    public partial string EditedArtist { get; set; } = "";

    [ObservableProperty]
    public partial string EditedYear { get; set; } = "";

    [ObservableProperty]
    public partial string EditedAlbum { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReload))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial string? PlaylistTitle { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImportPlaylist))]
    public partial string? PlaylistId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImportPlaylist))]
    public partial bool IsImportingPlaylist { get; set; }

    public bool CanImportPlaylist => !string.IsNullOrWhiteSpace(PlaylistId) && !IsImportingPlaylist;

    [ObservableProperty]
    public partial string? SourceQuery { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPause))]
    [NotifyPropertyChangedFor(nameof(CanResume))]
    public partial bool IsPaused { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowQualityChips))]
    [NotifyPropertyChangedFor(nameof(ShowAudioDetails))]
    [NotifyPropertyChangedFor(nameof(ShowSubtitleChoice))]
    public partial bool IsCompact { get; set; }

    [ObservableProperty]
    public partial string SubtitleChoice { get; set; } = "all";

    [ObservableProperty]
    public partial IReadOnlyList<string> SubtitleChoices { get; set; } = ["none", "all"];

    [ObservableProperty]
    public partial string? ClipStartText { get; set; }

    [ObservableProperty]
    public partial string? ClipEndText { get; set; }

    [ObservableProperty]
    public partial string? CoverTimeText { get; set; }

    [ObservableProperty]
    public partial bool VideoOnly { get; set; }

    [ObservableProperty]
    public partial bool SaveSrt { get; set; }

    [ObservableProperty]
    public partial bool Played { get; set; }

    public DownloadPauseGate PauseGate { get; } = new();

    public int AttemptCount { get; set; }

    public string FileNamePreview =>
        QueuePolicy.FileNameFromTitle(
            QueuePolicy.PreferredFileTitle(EditedArtist, EditedTitle, Video?.Title),
            SelectedContainer.Name
        );

    public string? TimeRemainingText
    {
        get
        {
            var downloaded = DownloadProgressMath.DownloadedBytes(
                Progress.Current.Fraction,
                TotalBytes
            );
            var remaining = QueuePolicy.TimeRemaining(TotalBytes, downloaded, BytesPerSecond);
            return remaining?.ToString(@"mm\:ss");
        }
    }

    public string EstimatedSizeText =>
        TotalBytes <= 0 ? "" : DownloadProgressMath.FormatMegabytes(TotalBytes);

    public bool CanReload => Status == DownloadStatus.Failed;

    public bool CanDownloadAgain => Status == DownloadStatus.Completed;

    public bool CanSaveAudio => Status == DownloadStatus.Completed && !IsAudioMode;

    public bool CanCopyLink => Video is not null;

    public string DurationText
    {
        get
        {
            if (Video?.Duration is not { } duration)
                return LocalizationManager.LiveLabel;

            return duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
                : $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
        }
    }

    public string ArtistText => AudioCardText.Artist(Video?.Author.ChannelTitle);

    public string GenreText => AudioCardText.Genre();

    public string DateText =>
        AudioCardText.Date(Video is YoutubeExplode.Videos.Video full ? full.UploadDate : null);

    private bool HasMatchingSelection =>
        IsAudioMode ? SelectedBitrateChoice is not null : QualityChips.Any(chip => chip.IsSelected);

    public void SetOptions(IReadOnlyList<VideoDownloadOption> options)
    {
        _options = options;
        IsLoading = false;
        RefreshChoices();
        TotalBytes =
            options
                .Where(option => !option.IsAudioOnly)
                .OrderByDescending(option => option.VideoQuality?.MaxHeight ?? 0)
                .FirstOrDefault()
                ?.StreamInfos.Sum(stream => stream.Size.Bytes)
            ?? 0;
        OnPropertyChanged(nameof(HasOptions));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(EstimatedSizeText));
        OnPropertyChanged(nameof(CanReload));
        OnPropertyChanged(nameof(CanDownloadAgain));
        OnPropertyChanged(nameof(CanSaveAudio));
        OnPropertyChanged(nameof(CanCopyLink));
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
    }

    public void SetSubtitleTracks(IEnumerable<string> languages)
    {
        SubtitleChoices = ["none", "all", .. languages.Distinct(StringComparer.OrdinalIgnoreCase)];
        if (!SubtitleChoices.Contains(SubtitleChoice))
            SubtitleChoice = "all";
    }

    [RelayCommand]
    private void PauseDownload()
    {
        PauseGate.Pause();
        IsPaused = true;
    }

    [RelayCommand]
    private void ResumeDownload()
    {
        PauseGate.Resume();
        IsPaused = false;
    }

    public void ApplyDefaults(bool isAudio, Container container, int? bitrateKbps)
    {
        if (Status != DownloadStatus.Ready)
            return;

        _refreshingChoices = true;
        IsAudioMode = isAudio;
        SelectedContainer = container;
        if (isAudio)
            _selectedBitrateKbps = bitrateKbps;
        _refreshingChoices = false;
        EnsureContainerMatchesMode();
        RefreshChoices();
    }

    partial void OnIsAudioModeChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(FormatChoices));
        if (_refreshingChoices || Status != DownloadStatus.Ready)
            return;

        EnsureContainerMatchesMode();
        RefreshChoices();
    }

    partial void OnSelectedContainerChanged(Container value)
    {
        if (_refreshingChoices || Status != DownloadStatus.Ready)
            return;

        RefreshChoices();
    }

    partial void OnSelectedBitrateChoiceChanged(BitrateChoice? value)
    {
        if (_refreshingChoices)
            return;

        _selectedBitrateKbps = value?.KilobitsPerSecond;
    }

    partial void OnStatusChanged(DownloadStatus value)
    {
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(ShowQualityChips));
        OnPropertyChanged(nameof(ShowAudioDetails));
        OnPropertyChanged(nameof(ShowSubtitleChoice));
        OnPropertyChanged(nameof(FillFraction));
        OnPropertyChanged(nameof(StatusAccentBrushKey));
        OnPropertyChanged(nameof(CanReload));
        OnPropertyChanged(nameof(CanDownloadAgain));
        OnPropertyChanged(nameof(CanSaveAudio));
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
        UpdateProgressSnapshot();
    }

    partial void OnIsPausedChanged(bool value) => OnPropertyChanged(nameof(StatusAccentBrushKey));

    partial void OnVideoChanged(IVideo? value)
    {
        OnPropertyChanged(nameof(DurationText));
        OnPropertyChanged(nameof(ArtistText));
        OnPropertyChanged(nameof(DateText));
        if (value is not null && QueuePolicy.IsPlaceholderTitle(EditedTitle, SourceQuery))
            EditedTitle = value.Title;
        if (string.IsNullOrWhiteSpace(EditedArtist))
            EditedArtist = value?.Author.ChannelTitle ?? "";
        if (value is YoutubeExplode.Videos.Video full && string.IsNullOrWhiteSpace(EditedYear))
            EditedYear = full.UploadDate.Year.ToString();
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void SelectQuality(QualityChip? chip)
    {
        if (chip is null || Status != DownloadStatus.Ready)
            return;

        _selectedHeight = chip.Height;
        RefreshChoices();
    }

    public bool TryLockSelection()
    {
        if (!CanStart)
            return false;

        if (IsAudioMode)
        {
            var choice = SelectedBitrateChoice ?? BitrateChoice.Source;
            var audio = _options
                .Where(option => option.IsAudioOnly && option.Container == SelectedContainer)
                .ToArray();
            if (audio.Length == 0)
            {
                audio = _options
                    .Where(option => option.IsAudioOnly)
                    .OrderByDescending(option => option.SourceAudioBitrate?.BitsPerSecond ?? 0)
                    .ToArray();
            }

            var kbps = audio
                .Select(option =>
                    BitrateChoiceList.RoundKbps(option.SourceAudioBitrate?.KiloBitsPerSecond ?? 0)
                )
                .ToArray();
            var index = BitrateChoiceList.PickAudioIndex(kbps, choice);
            if (index < 0)
                return false;

            DownloadOption = audio[index] with { Container = SelectedContainer };
            AudioBitrate = BitrateChoiceList.ToPreference(choice);
        }
        else
        {
            var selected = QualityChips.FirstOrDefault(chip => chip.IsSelected);
            var videos = _options.Where(option => !option.IsAudioOnly).ToArray();
            if (selected is null || videos.Length == 0)
                return false;

            var match =
                videos
                    .Where(option => option.VideoQuality?.MaxHeight == selected.Height)
                    .OrderByDescending(option => option.Container == SelectedContainer)
                    .ThenByDescending(option => option.StreamInfos.Sum(stream => stream.Size.Bytes))
                    .ThenBy(option => option.IsVideoUpscaled)
                    .FirstOrDefault()
                ?? videos[0];

            if (VideoOnly)
            {
                var videoStreams = match
                    .StreamInfos.Where(stream => stream is not IAudioStreamInfo)
                    .ToArray();
                DownloadOption = match with
                {
                    Container = SelectedContainer,
                    StreamInfos = videoStreams,
                };
            }
            else
            {
                DownloadOption = match with { Container = SelectedContainer };
            }

            AudioBitrate = AudioBitratePreference.Source;
        }

        TotalBytes = DownloadOption.StreamInfos.Sum(stream => stream.Size.Bytes);
        return true;
    }

    public void CaptureTotalBytes()
    {
        if (DownloadOption is null)
            return;

        TotalBytes = DownloadOption.StreamInfos.Sum(stream => stream.Size.Bytes);
    }

    private void EnsureContainerMatchesMode()
    {
        if (FormatChoices.Contains(SelectedContainer))
            return;

        SelectedContainer = FormatChoices[0];
    }

    public void RefreshChoicesPublic() => RefreshChoices();

    private void RefreshChoices()
    {
        _refreshingChoices = true;

        var snapshots = _options.Select(option => new QualityOptionSnapshot(
            option.VideoQuality?.Label ?? option.Container.Name,
            option.VideoQuality?.MaxHeight ?? 0,
            option.Container.Name,
            option.IsAudioOnly,
            option.IsVideoUpscaled
        ));
        QualityChips = QualityChipList.Build(snapshots, container: null, _selectedHeight);
        _selectedHeight = QualityChips.FirstOrDefault(chip => chip.IsSelected)?.Height;

        var nativeRates = _options
            .Where(option => option.IsAudioOnly)
            .Select(option => option.SourceAudioBitrate?.KiloBitsPerSecond ?? 0)
            .Where(rate => rate > 0);
        BitrateChoices = BitrateChoiceList.Build(nativeRates);
        SelectedBitrateChoice =
            BitrateChoices.FirstOrDefault(choice =>
                choice.KilobitsPerSecond == _selectedBitrateKbps
            ) ?? BitrateChoices[0];
        _selectedBitrateKbps = SelectedBitrateChoice.KilobitsPerSecond;

        _refreshingChoices = false;
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(ShowQualityChips));
    }

    private void UpdateProgressSnapshot()
    {
        var downloaded = DownloadProgressMath.DownloadedBytes(
            Progress.Current.Fraction,
            TotalBytes
        );
        var now = DateTimeOffset.UtcNow;
        if (_lastSampledAt == default)
        {
            _lastSampledAt = now;
            _lastSampledBytes = downloaded;
        }
        else
        {
            var elapsed = now - _lastSampledAt;
            if (elapsed.TotalMilliseconds >= 200)
            {
                BytesPerSecond = DownloadSpeed.BytesPerSecond(
                    downloaded - _lastSampledBytes,
                    elapsed
                );
                _lastSampledBytes = downloaded;
                _lastSampledAt = now;
            }
        }

        ProgressLabel = Status switch
        {
            DownloadStatus.Enqueued => LocalizationManager.WaitingLabel,
            DownloadStatus.Started => string.Format(
                LocalizationManager.DownloadedProgress,
                DownloadProgressMath.FormatMegabytes(downloaded),
                DownloadProgressMath.FormatMegabytes(TotalBytes)
            ),
            DownloadStatus.Completed => LocalizationManager.DownloadStatusCompleted,
            DownloadStatus.Failed => LocalizationManager.DownloadStatusFailed,
            DownloadStatus.Canceled => LocalizationManager.DownloadStatusCanceled,
            _ => "",
        };

        OnPropertyChanged(nameof(FillFraction));
    }
}
