using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PowerKit.Extensions;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Utils;
using YoutubeDownloader.Framework;
using YoutubeDownloader.Localization;
using YoutubeDownloader.Services;

namespace YoutubeDownloader.ViewModels.Dialogs;

public partial class SettingsViewModel : DialogViewModelBase
{
    private readonly DialogManager _dialogManager;
    private readonly SettingsService _settingsService;
    private readonly IDisposable _eventSubscription;
    private SettingsSnapshot? _snapshot;

    public SettingsViewModel(
        DialogManager dialogManager,
        LocalizationManager localizationManager,
        SettingsService settingsService
    )
    {
        _dialogManager = dialogManager;
        LocalizationManager = localizationManager;
        _settingsService = settingsService;
        _eventSubscription = _settingsService.WatchAllProperties(OnAllPropertiesChanged);
        SelectedTab = SettingsTab.General;
        _snapshot = SettingsSnapshot.Capture(settingsService);
    }

    public LocalizationManager LocalizationManager { get; }

    public IReadOnlyList<SettingsTab> Tabs { get; } = Enum.GetValues<SettingsTab>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGeneralTab))]
    [NotifyPropertyChangedFor(nameof(IsDownloadTab))]
    [NotifyPropertyChangedFor(nameof(IsNetworkTab))]
    [NotifyPropertyChangedFor(nameof(IsAudioTab))]
    [NotifyPropertyChangedFor(nameof(IsVideoTab))]
    [NotifyPropertyChangedFor(nameof(IsTagsTab))]
    [NotifyPropertyChangedFor(nameof(IsAuthorizationTab))]
    public partial SettingsTab SelectedTab { get; set; }

    public bool IsGeneralTab => SelectedTab == SettingsTab.General;
    public bool IsDownloadTab => SelectedTab == SettingsTab.Download;
    public bool IsNetworkTab => SelectedTab == SettingsTab.Network;
    public bool IsAudioTab => SelectedTab == SettingsTab.Audio;
    public bool IsVideoTab => SelectedTab == SettingsTab.Video;
    public bool IsTagsTab => SelectedTab == SettingsTab.Tags;
    public bool IsAuthorizationTab => SelectedTab == SettingsTab.Authorization;

    [ObservableProperty]
    public partial string? ProxyTestResult { get; set; }

    [ObservableProperty]
    public partial bool IsProxyTesting { get; set; }

    public IReadOnlyList<ThemeVariant> AvailableThemes { get; } = Enum.GetValues<ThemeVariant>();

    public ThemeVariant Theme
    {
        get => _settingsService.Theme;
        set => _settingsService.Theme = value;
    }

    public IReadOnlyList<Language> AvailableLanguages { get; } = Enum.GetValues<Language>();

    public Language Language
    {
        get => _settingsService.Language;
        set => _settingsService.Language = value;
    }

    public bool IsAutoUpdateAvailable { get; } =
        OperatingSystem.IsWindows() && StartOptions.Current.IsAutoUpdateAllowed;

    public bool IsAutoUpdateEnabled
    {
        get => _settingsService.IsAutoUpdateEnabled;
        set => _settingsService.IsAutoUpdateEnabled = value;
    }

    public bool IsAuthPersisted
    {
        get => _settingsService.IsAuthPersisted;
        set => _settingsService.IsAuthPersisted = value;
    }

    public string? FFmpegFilePath
    {
        get => _settingsService.FFmpegFilePath;
        set => _settingsService.FFmpegFilePath = !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    public bool ShouldInjectLanguageSpecificAudioStreams
    {
        get => _settingsService.ShouldInjectLanguageSpecificAudioStreams;
        set => _settingsService.ShouldInjectLanguageSpecificAudioStreams = value;
    }

    public bool ShouldInjectSubtitles
    {
        get => _settingsService.ShouldInjectSubtitles;
        set => _settingsService.ShouldInjectSubtitles = value;
    }

    public bool ShouldInjectTags
    {
        get => _settingsService.ShouldInjectTags;
        set => _settingsService.ShouldInjectTags = value;
    }

    public bool ShouldSkipExistingFiles
    {
        get => _settingsService.ShouldSkipExistingFiles;
        set => _settingsService.ShouldSkipExistingFiles = value;
    }

    public string FileNameTemplate
    {
        get => _settingsService.FileNameTemplate;
        set => _settingsService.FileNameTemplate = value;
    }

    public string VideoFileNameTemplate
    {
        get => _settingsService.VideoFileNameTemplate;
        set => _settingsService.VideoFileNameTemplate = value;
    }

    public string AudioFileNameTemplate
    {
        get => _settingsService.AudioFileNameTemplate;
        set => _settingsService.AudioFileNameTemplate = value;
    }

    public int ParallelLimit
    {
        get => _settingsService.ParallelLimit;
        set => _settingsService.ParallelLimit = QueuePolicy.ClampSimultaneous(value);
    }

    public bool IsProxyEnabled
    {
        get => _settingsService.IsProxyEnabled;
        set => _settingsService.IsProxyEnabled = value;
    }

    public IReadOnlyList<ProxyProtocol> AvailableProxyProtocols { get; } =
        Enum.GetValues<ProxyProtocol>();

    public ProxyProtocol ProxyProtocol
    {
        get => _settingsService.ProxyProtocol;
        set => _settingsService.ProxyProtocol = value;
    }

    public string? ProxyHost
    {
        get => _settingsService.ProxyHost;
        set => _settingsService.ProxyHost = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public int ProxyPort
    {
        get => _settingsService.ProxyPort;
        set => _settingsService.ProxyPort = Math.Clamp(value, 1, 65535);
    }

    public string? ProxyUsername
    {
        get => _settingsService.ProxyUsername;
        set =>
            _settingsService.ProxyUsername = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public string? ProxyPassword
    {
        get => _settingsService.ProxyPassword;
        set => _settingsService.ProxyPassword = string.IsNullOrEmpty(value) ? null : value;
    }

    public string? VideoFolder
    {
        get => _settingsService.VideoFolder;
        set => _settingsService.VideoFolder = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public string? AudioFolder
    {
        get => _settingsService.AudioFolder;
        set => _settingsService.AudioFolder = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public bool VideoUsesAudioFolder
    {
        get => _settingsService.VideoUsesAudioFolder;
        set => _settingsService.VideoUsesAudioFolder = value;
    }

    public bool PlaylistSubfolderVideo
    {
        get => _settingsService.PlaylistSubfolderVideo;
        set => _settingsService.PlaylistSubfolderVideo = value;
    }

    public bool PlaylistSubfolderAudio
    {
        get => _settingsService.PlaylistSubfolderAudio;
        set => _settingsService.PlaylistSubfolderAudio = value;
    }

    public bool AddCardsOnTop
    {
        get => _settingsService.AddCardsOnTop;
        set => _settingsService.AddCardsOnTop = value;
    }

    public bool AutoStartReady
    {
        get => _settingsService.AutoStartReady;
        set => _settingsService.AutoStartReady = value;
    }

    public bool AutoRemoveCompleted
    {
        get => _settingsService.AutoRemoveCompleted;
        set => _settingsService.AutoRemoveCompleted = value;
    }

    public bool AutoExpandPlaylist
    {
        get => _settingsService.AutoExpandPlaylist;
        set => _settingsService.AutoExpandPlaylist = value;
    }

    public bool AutoAddClipboardLinks
    {
        get => _settingsService.AutoAddClipboardLinks;
        set => _settingsService.AutoAddClipboardLinks = value;
    }

    public bool NotifyAdded
    {
        get => _settingsService.NotifyAdded;
        set => _settingsService.NotifyAdded = value;
    }

    public bool NotifyStarted
    {
        get => _settingsService.NotifyStarted;
        set => _settingsService.NotifyStarted = value;
    }

    public bool NotifyCompleted
    {
        get => _settingsService.NotifyCompleted;
        set => _settingsService.NotifyCompleted = value;
    }

    public bool NotifyQueueDone
    {
        get => _settingsService.NotifyQueueDone;
        set => _settingsService.NotifyQueueDone = value;
    }

    public bool MinimizeToTray
    {
        get => _settingsService.MinimizeToTray;
        set => _settingsService.MinimizeToTray = value;
    }

    public bool LoudnormEnabled
    {
        get => _settingsService.LoudnormEnabled;
        set => _settingsService.LoudnormEnabled = value;
    }

    public int ConnectionsPerDownload
    {
        get => _settingsService.ConnectionsPerDownload;
        set => _settingsService.ConnectionsPerDownload = Math.Clamp(value, 1, 16);
    }

    public bool RemoveEmojiFromNames
    {
        get => _settingsService.RemoveEmojiFromNames;
        set => _settingsService.RemoveEmojiFromNames = value;
    }

    public bool PreferHigherBitrate
    {
        get => _settingsService.PreferHigherBitrate;
        set => _settingsService.PreferHigherBitrate = value;
    }

    public string YearTagSource
    {
        get => _settingsService.YearTagSource;
        set => _settingsService.YearTagSource = value;
    }

    public IReadOnlyList<string> YearTagSources { get; } = ["Upload", "MusicBrainz", "None"];

    public string? DefaultAlbumArtist
    {
        get => _settingsService.DefaultAlbumArtist;
        set => _settingsService.DefaultAlbumArtist = value;
    }

    public bool WriteDescriptionAsComment
    {
        get => _settingsService.WriteDescriptionAsComment;
        set => _settingsService.WriteDescriptionAsComment = value;
    }

    public bool WriteArtwork
    {
        get => _settingsService.WriteArtwork;
        set => _settingsService.WriteArtwork = value;
    }

    public bool PlaylistAlbumTags
    {
        get => _settingsService.PlaylistAlbumTags;
        set => _settingsService.PlaylistAlbumTags = value;
    }

    public bool UseChannelAsArtist
    {
        get => _settingsService.UseChannelAsArtist;
        set => _settingsService.UseChannelAsArtist = value;
    }

    public bool CleanTagQuotesEmoji
    {
        get => _settingsService.CleanTagQuotesEmoji;
        set => _settingsService.CleanTagQuotesEmoji = value;
    }

    public bool PauseOnMetered
    {
        get => _settingsService.PauseOnMetered;
        set => _settingsService.PauseOnMetered = value;
    }

    public bool PreventSleepWhileDownloading
    {
        get => _settingsService.PreventSleepWhileDownloading;
        set => _settingsService.PreventSleepWhileDownloading = value;
    }

    public bool PlayOnComplete
    {
        get => _settingsService.PlayOnComplete;
        set => _settingsService.PlayOnComplete = value;
    }

    public int MaxQueueSize
    {
        get => _settingsService.MaxQueueSize;
        set => _settingsService.MaxQueueSize = Math.Max(0, value);
    }

    public string? BackupFolder
    {
        get => _settingsService.BackupFolder;
        set => _settingsService.BackupFolder = value;
    }

    public bool SponsorBlockEnabled
    {
        get => _settingsService.SponsorBlockEnabled;
        set => _settingsService.SponsorBlockEnabled = value;
    }

    public bool SaveSubtitlesAsSrt
    {
        get => _settingsService.SaveSubtitlesAsSrt;
        set => _settingsService.SaveSubtitlesAsSrt = value;
    }

    public bool HasAuthCookies => _settingsService.LastAuthCookies is { Count: > 0 };

    public int HistoryRetentionDays
    {
        get => _settingsService.HistoryRetentionDays;
        set => _settingsService.HistoryRetentionDays = value is 0 or 30 ? value : 0;
    }

    [RelayCommand]
    private async Task BrowseFFmpegFilePathAsync()
    {
        var fileTypes = OperatingSystem.IsWindows()
            ? new[]
            {
                new FilePickerFileType("FFmpeg executable") { Patterns = ["*.exe"] },
                FilePickerFileTypes.All,
            }
            : null;

        var filePath = await _dialogManager.PromptOpenFilePathAsync(fileTypes);
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        FFmpegFilePath = filePath;
    }

    [RelayCommand]
    private void ResetFFmpegFilePath() => FFmpegFilePath = null;

    [RelayCommand]
    private async Task BrowseVideoFolderAsync()
    {
        var path = await _dialogManager.PromptDirectoryPathAsync(VideoFolder ?? "");
        if (!string.IsNullOrWhiteSpace(path))
            VideoFolder = path;
    }

    [RelayCommand]
    private async Task BrowseAudioFolderAsync()
    {
        var path = await _dialogManager.PromptDirectoryPathAsync(AudioFolder ?? "");
        if (!string.IsNullOrWhiteSpace(path))
            AudioFolder = path;
    }

    [RelayCommand]
    private async Task BrowseBackupFolderAsync()
    {
        var path = await _dialogManager.PromptDirectoryPathAsync(BackupFolder ?? "");
        if (!string.IsNullOrWhiteSpace(path))
            BackupFolder = path;
    }

    [RelayCommand]
    private async Task TestProxyAsync()
    {
        IsProxyTesting = true;
        ProxyTestResult = LocalizationManager.ProxyTestingLabel;
        try
        {
            _settingsService.ApplyProxy();
            using var response = await Http.Client.GetAsync("https://www.youtube.com/generate_204");
            ProxyTestResult = QueuePolicy.IsProxyTestSuccess((int)response.StatusCode)
                ? LocalizationManager.ProxyTestSuccessLabel
                : $"{LocalizationManager.ProxyTestFailedLabel}: {(int)response.StatusCode}";
        }
        catch (Exception ex)
        {
            ProxyTestResult = $"{LocalizationManager.ProxyTestFailedLabel}: {ex.Message}";
        }
        finally
        {
            IsProxyTesting = false;
        }
    }

    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        var path = await _dialogManager.PromptSaveFilePathAsync(
            [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
            "YoutubeDownloader.settings.json"
        );
        if (string.IsNullOrWhiteSpace(path))
            return;

        var values = new Dictionary<string, object?>
        {
            ["Theme"] = Theme.ToString(),
            ["Language"] = Language.ToString(),
            ["ParallelLimit"] = ParallelLimit,
            ["VideoFolder"] = VideoFolder,
            ["AudioFolder"] = AudioFolder,
            ["VideoUsesAudioFolder"] = VideoUsesAudioFolder,
            ["IsProxyEnabled"] = IsProxyEnabled,
            ["ProxyProtocol"] = ProxyProtocol.ToString(),
            ["ProxyHost"] = ProxyHost,
            ["ProxyPort"] = ProxyPort,
            ["LoudnormEnabled"] = LoudnormEnabled,
            ["ShouldInjectTags"] = ShouldInjectTags,
            ["YearTagSource"] = YearTagSource,
            ["SponsorBlockEnabled"] = SponsorBlockEnabled,
            ["MinimizeToTray"] = MinimizeToTray,
        };

        await File.WriteAllTextAsync(path, SettingsExport.ToJson(values));
    }

    [RelayCommand]
    private async Task ImportSettingsAsync()
    {
        var path = await _dialogManager.PromptOpenFilePathAsync([
            new FilePickerFileType("JSON") { Patterns = ["*.json"] },
        ]);
        if (string.IsNullOrWhiteSpace(path))
            return;

        var json = await File.ReadAllTextAsync(path);
        var values = SettingsExport.FromJson(json);
        if (
            values.TryGetValue("ParallelLimit", out var parallel)
            && parallel.TryGetInt32(out var limit)
        )
            ParallelLimit = limit;
        if (values.TryGetValue("VideoFolder", out var videoFolder))
            VideoFolder = videoFolder.GetString();
        if (values.TryGetValue("AudioFolder", out var audioFolder))
            AudioFolder = audioFolder.GetString();
        if (
            values.TryGetValue("VideoUsesAudioFolder", out var same)
            && same.ValueKind is JsonValueKind.True or JsonValueKind.False
        )
            VideoUsesAudioFolder = same.GetBoolean();
        if (
            values.TryGetValue("LoudnormEnabled", out var loud)
            && loud.ValueKind is JsonValueKind.True or JsonValueKind.False
        )
            LoudnormEnabled = loud.GetBoolean();
        if (
            values.TryGetValue("SponsorBlockEnabled", out var sponsor)
            && sponsor.ValueKind is JsonValueKind.True or JsonValueKind.False
        )
            SponsorBlockEnabled = sponsor.GetBoolean();
        if (
            values.TryGetValue("MinimizeToTray", out var tray)
            && tray.ValueKind is JsonValueKind.True or JsonValueKind.False
        )
            MinimizeToTray = tray.GetBoolean();
        if (values.TryGetValue("YearTagSource", out var year))
            YearTagSource = year.GetString() ?? YearTagSource;
        OnAllPropertiesChanged();
    }

    [RelayCommand]
    private void ClearThumbnailCache()
    {
        try
        {
            var cache = Path.Combine(Path.GetTempPath(), "YoutubeDownloader", "thumbnails");
            if (Directory.Exists(cache))
                Directory.Delete(cache, recursive: true);
        }
        catch
        {
            // Ignore
        }
    }

    [RelayCommand]
    private void ImportBrowserCookies()
    {
        var cookies = BrowserCookieReader.TryReadYouTubeCookies();
        if (cookies.Count > 0)
            _settingsService.LastAuthCookies = cookies;
        OnPropertyChanged(nameof(HasAuthCookies));
    }

    [RelayCommand]
    private void Apply()
    {
        _settingsService.Save();
        _snapshot = SettingsSnapshot.Capture(_settingsService);
    }

    [RelayCommand]
    private void Ok()
    {
        Apply();
        Close(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        _snapshot?.Restore(_settingsService);
        Close(false);
    }

    [RelayCommand]
    private void RestoreTabDefaults()
    {
        switch (SelectedTab)
        {
            case SettingsTab.General:
                Theme = default;
                Language = default;
                IsAutoUpdateEnabled = true;
                AddCardsOnTop = true;
                AutoStartReady = false;
                AutoRemoveCompleted = false;
                AutoExpandPlaylist = true;
                AutoAddClipboardLinks = false;
                NotifyAdded = true;
                NotifyStarted = false;
                NotifyCompleted = true;
                NotifyQueueDone = true;
                MinimizeToTray = false;
                break;
            case SettingsTab.Download:
                ParallelLimit = 2;
                VideoFolder = null;
                AudioFolder = null;
                VideoUsesAudioFolder = false;
                PlaylistSubfolderVideo = false;
                PlaylistSubfolderAudio = false;
                PreventSleepWhileDownloading = false;
                MaxQueueSize = 0;
                BackupFolder = null;
                PlayOnComplete = false;
                PauseOnMetered = true;
                break;
            case SettingsTab.Network:
                IsProxyEnabled = false;
                ProxyProtocol = ProxyProtocol.Http;
                ProxyHost = null;
                ProxyPort = 8080;
                ProxyUsername = null;
                ProxyPassword = null;
                ConnectionsPerDownload = 3;
                break;
            case SettingsTab.Audio:
                AudioFileNameTemplate = "$title";
                ShouldSkipExistingFiles = false;
                RemoveEmojiFromNames = false;
                LoudnormEnabled = false;
                break;
            case SettingsTab.Video:
                VideoFileNameTemplate = "$title";
                PreferHigherBitrate = true;
                ShouldInjectSubtitles = true;
                SaveSubtitlesAsSrt = false;
                SponsorBlockEnabled = false;
                break;
            case SettingsTab.Tags:
                ShouldInjectTags = true;
                YearTagSource = "Upload";
                DefaultAlbumArtist = null;
                WriteDescriptionAsComment = false;
                WriteArtwork = true;
                PlaylistAlbumTags = true;
                UseChannelAsArtist = true;
                CleanTagQuotesEmoji = false;
                break;
            case SettingsTab.Authorization:
                IsAuthPersisted = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnAllPropertiesChanged();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _eventSubscription.Dispose();

        base.Dispose(disposing);
    }

    private sealed class SettingsSnapshot
    {
        public required ThemeVariant Theme { get; init; }
        public required Language Language { get; init; }
        public required bool IsAutoUpdateEnabled { get; init; }
        public required bool IsAuthPersisted { get; init; }
        public required string? FFmpegFilePath { get; init; }
        public required bool ShouldInjectLanguageSpecificAudioStreams { get; init; }
        public required bool ShouldInjectSubtitles { get; init; }
        public required bool ShouldInjectTags { get; init; }
        public required bool ShouldSkipExistingFiles { get; init; }
        public required string FileNameTemplate { get; init; }
        public required string VideoFileNameTemplate { get; init; }
        public required string AudioFileNameTemplate { get; init; }
        public required int ParallelLimit { get; init; }
        public required bool IsProxyEnabled { get; init; }
        public required ProxyProtocol ProxyProtocol { get; init; }
        public required string? ProxyHost { get; init; }
        public required int ProxyPort { get; init; }
        public required string? ProxyUsername { get; init; }
        public required string? ProxyPassword { get; init; }
        public required string? VideoFolder { get; init; }
        public required string? AudioFolder { get; init; }
        public required bool VideoUsesAudioFolder { get; init; }
        public required bool PlaylistSubfolderVideo { get; init; }
        public required bool PlaylistSubfolderAudio { get; init; }
        public required bool AddCardsOnTop { get; init; }
        public required bool AutoStartReady { get; init; }
        public required bool AutoRemoveCompleted { get; init; }
        public required bool AutoExpandPlaylist { get; init; }
        public required bool AutoAddClipboardLinks { get; init; }
        public required bool NotifyAdded { get; init; }
        public required bool NotifyStarted { get; init; }
        public required bool NotifyCompleted { get; init; }
        public required bool NotifyQueueDone { get; init; }
        public required bool MinimizeToTray { get; init; }
        public required bool LoudnormEnabled { get; init; }
        public required int ConnectionsPerDownload { get; init; }
        public required bool RemoveEmojiFromNames { get; init; }
        public required bool PreferHigherBitrate { get; init; }
        public required string YearTagSource { get; init; }
        public required string? DefaultAlbumArtist { get; init; }
        public required bool WriteDescriptionAsComment { get; init; }
        public required bool WriteArtwork { get; init; }
        public required bool PlaylistAlbumTags { get; init; }
        public required bool UseChannelAsArtist { get; init; }
        public required bool CleanTagQuotesEmoji { get; init; }
        public required bool PauseOnMetered { get; init; }
        public required bool PreventSleepWhileDownloading { get; init; }
        public required bool PlayOnComplete { get; init; }
        public required int MaxQueueSize { get; init; }
        public required string? BackupFolder { get; init; }
        public required bool SponsorBlockEnabled { get; init; }
        public required bool SaveSubtitlesAsSrt { get; init; }

        public static SettingsSnapshot Capture(SettingsService s) =>
            new()
            {
                Theme = s.Theme,
                Language = s.Language,
                IsAutoUpdateEnabled = s.IsAutoUpdateEnabled,
                IsAuthPersisted = s.IsAuthPersisted,
                FFmpegFilePath = s.FFmpegFilePath,
                ShouldInjectLanguageSpecificAudioStreams =
                    s.ShouldInjectLanguageSpecificAudioStreams,
                ShouldInjectSubtitles = s.ShouldInjectSubtitles,
                ShouldInjectTags = s.ShouldInjectTags,
                ShouldSkipExistingFiles = s.ShouldSkipExistingFiles,
                FileNameTemplate = s.FileNameTemplate,
                VideoFileNameTemplate = s.VideoFileNameTemplate,
                AudioFileNameTemplate = s.AudioFileNameTemplate,
                ParallelLimit = s.ParallelLimit,
                IsProxyEnabled = s.IsProxyEnabled,
                ProxyProtocol = s.ProxyProtocol,
                ProxyHost = s.ProxyHost,
                ProxyPort = s.ProxyPort,
                ProxyUsername = s.ProxyUsername,
                ProxyPassword = s.ProxyPassword,
                VideoFolder = s.VideoFolder,
                AudioFolder = s.AudioFolder,
                VideoUsesAudioFolder = s.VideoUsesAudioFolder,
                PlaylistSubfolderVideo = s.PlaylistSubfolderVideo,
                PlaylistSubfolderAudio = s.PlaylistSubfolderAudio,
                AddCardsOnTop = s.AddCardsOnTop,
                AutoStartReady = s.AutoStartReady,
                AutoRemoveCompleted = s.AutoRemoveCompleted,
                AutoExpandPlaylist = s.AutoExpandPlaylist,
                AutoAddClipboardLinks = s.AutoAddClipboardLinks,
                NotifyAdded = s.NotifyAdded,
                NotifyStarted = s.NotifyStarted,
                NotifyCompleted = s.NotifyCompleted,
                NotifyQueueDone = s.NotifyQueueDone,
                MinimizeToTray = s.MinimizeToTray,
                LoudnormEnabled = s.LoudnormEnabled,
                ConnectionsPerDownload = s.ConnectionsPerDownload,
                RemoveEmojiFromNames = s.RemoveEmojiFromNames,
                PreferHigherBitrate = s.PreferHigherBitrate,
                YearTagSource = s.YearTagSource,
                DefaultAlbumArtist = s.DefaultAlbumArtist,
                WriteDescriptionAsComment = s.WriteDescriptionAsComment,
                WriteArtwork = s.WriteArtwork,
                PlaylistAlbumTags = s.PlaylistAlbumTags,
                UseChannelAsArtist = s.UseChannelAsArtist,
                CleanTagQuotesEmoji = s.CleanTagQuotesEmoji,
                PauseOnMetered = s.PauseOnMetered,
                PreventSleepWhileDownloading = s.PreventSleepWhileDownloading,
                PlayOnComplete = s.PlayOnComplete,
                MaxQueueSize = s.MaxQueueSize,
                BackupFolder = s.BackupFolder,
                SponsorBlockEnabled = s.SponsorBlockEnabled,
                SaveSubtitlesAsSrt = s.SaveSubtitlesAsSrt,
            };

        public void Restore(SettingsService s)
        {
            s.Theme = Theme;
            s.Language = Language;
            s.IsAutoUpdateEnabled = IsAutoUpdateEnabled;
            s.IsAuthPersisted = IsAuthPersisted;
            s.FFmpegFilePath = FFmpegFilePath;
            s.ShouldInjectLanguageSpecificAudioStreams = ShouldInjectLanguageSpecificAudioStreams;
            s.ShouldInjectSubtitles = ShouldInjectSubtitles;
            s.ShouldInjectTags = ShouldInjectTags;
            s.ShouldSkipExistingFiles = ShouldSkipExistingFiles;
            s.FileNameTemplate = FileNameTemplate;
            s.VideoFileNameTemplate = VideoFileNameTemplate;
            s.AudioFileNameTemplate = AudioFileNameTemplate;
            s.ParallelLimit = ParallelLimit;
            s.IsProxyEnabled = IsProxyEnabled;
            s.ProxyProtocol = ProxyProtocol;
            s.ProxyHost = ProxyHost;
            s.ProxyPort = ProxyPort;
            s.ProxyUsername = ProxyUsername;
            s.ProxyPassword = ProxyPassword;
            s.VideoFolder = VideoFolder;
            s.AudioFolder = AudioFolder;
            s.VideoUsesAudioFolder = VideoUsesAudioFolder;
            s.PlaylistSubfolderVideo = PlaylistSubfolderVideo;
            s.PlaylistSubfolderAudio = PlaylistSubfolderAudio;
            s.AddCardsOnTop = AddCardsOnTop;
            s.AutoStartReady = AutoStartReady;
            s.AutoRemoveCompleted = AutoRemoveCompleted;
            s.AutoExpandPlaylist = AutoExpandPlaylist;
            s.AutoAddClipboardLinks = AutoAddClipboardLinks;
            s.NotifyAdded = NotifyAdded;
            s.NotifyStarted = NotifyStarted;
            s.NotifyCompleted = NotifyCompleted;
            s.NotifyQueueDone = NotifyQueueDone;
            s.MinimizeToTray = MinimizeToTray;
            s.LoudnormEnabled = LoudnormEnabled;
            s.ConnectionsPerDownload = ConnectionsPerDownload;
            s.RemoveEmojiFromNames = RemoveEmojiFromNames;
            s.PreferHigherBitrate = PreferHigherBitrate;
            s.YearTagSource = YearTagSource;
            s.DefaultAlbumArtist = DefaultAlbumArtist;
            s.WriteDescriptionAsComment = WriteDescriptionAsComment;
            s.WriteArtwork = WriteArtwork;
            s.PlaylistAlbumTags = PlaylistAlbumTags;
            s.UseChannelAsArtist = UseChannelAsArtist;
            s.CleanTagQuotesEmoji = CleanTagQuotesEmoji;
            s.PauseOnMetered = PauseOnMetered;
            s.PreventSleepWhileDownloading = PreventSleepWhileDownloading;
            s.PlayOnComplete = PlayOnComplete;
            s.MaxQueueSize = MaxQueueSize;
            s.BackupFolder = BackupFolder;
            s.SponsorBlockEnabled = SponsorBlockEnabled;
            s.SaveSubtitlesAsSrt = SaveSubtitlesAsSrt;
        }
    }
}

public enum SettingsTab
{
    General,
    Download,
    Network,
    Audio,
    Video,
    Tags,
    Authorization,
}
