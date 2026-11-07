using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using Cogwheel;
using CommunityToolkit.Mvvm.ComponentModel;
using YoutubeDownloader.XC.Core.Downloading;
using YoutubeDownloader.XC.Core.Utils;
using YoutubeDownloader.XC.Framework;
using YoutubeDownloader.XC.Localization;
using Container = YoutubeExplode.Videos.Streams.Container;

namespace YoutubeDownloader.XC.Services;

[ObservableObject]
public partial class SettingsService()
    : SettingsBase(StartOptions.Current.SettingsPath, SettingsSerializerContext.Default)
{
    [ObservableProperty]
    public partial ThemeVariant Theme { get; set; }

    [ObservableProperty]
    public partial Language Language { get; set; }

    [ObservableProperty]
    public partial bool IsAutoUpdateEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAuthPersisted { get; set; } = true;

    [ObservableProperty]
    public partial string? FFmpegFilePath { get; set; }

    [ObservableProperty]
    public partial bool ShouldInjectLanguageSpecificAudioStreams { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldInjectSubtitles { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldInjectTags { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldSkipExistingFiles { get; set; }

    [ObservableProperty]
    public partial string FileNameTemplate { get; set; } = "$title";

    [ObservableProperty]
    public partial string VideoFileNameTemplate { get; set; } = "$title";

    [ObservableProperty]
    public partial string AudioFileNameTemplate { get; set; } = "$title";

    [ObservableProperty]
    public partial int ParallelLimit { get; set; } = 2;

    [ObservableProperty]
    public partial bool IsProxyEnabled { get; set; }

    [ObservableProperty]
    public partial ProxyProtocol ProxyProtocol { get; set; } = ProxyProtocol.Http;

    [ObservableProperty]
    public partial string? ProxyHost { get; set; }

    [ObservableProperty]
    public partial int ProxyPort { get; set; } = 8080;

    [ObservableProperty]
    public partial string? ProxyUsername { get; set; }

    [ObservableProperty]
    [JsonConverter(typeof(ProxyPasswordEncryptionConverter))]
    public partial string? ProxyPassword { get; set; }

    [ObservableProperty]
    public partial DownloadMediaKind LastDownloadMediaKind { get; set; } = DownloadMediaKind.Video;

    [ObservableProperty]
    public partial AudioBitratePreference LastAudioBitratePreference { get; set; } =
        AudioBitratePreference.Source;

    [ObservableProperty]
    public partial string? LastOutputDirectory { get; set; }

    [ObservableProperty]
    public partial string? VideoFolder { get; set; }

    [ObservableProperty]
    public partial string? AudioFolder { get; set; }

    [ObservableProperty]
    public partial bool VideoUsesAudioFolder { get; set; }

    [ObservableProperty]
    public partial bool PlaylistSubfolderVideo { get; set; }

    [ObservableProperty]
    public partial bool PlaylistSubfolderAudio { get; set; }

    [ObservableProperty]
    public partial bool AddCardsOnTop { get; set; } = true;

    [ObservableProperty]
    public partial bool AutoStartReady { get; set; }

    [ObservableProperty]
    public partial bool AutoRemoveCompleted { get; set; }

    [ObservableProperty]
    public partial bool AutoExpandPlaylist { get; set; } = true;

    [ObservableProperty]
    public partial bool AutoAddClipboardLinks { get; set; }

    [ObservableProperty]
    public partial bool NotifyAdded { get; set; } = true;

    [ObservableProperty]
    public partial bool NotifyStarted { get; set; }

    [ObservableProperty]
    public partial bool NotifyCompleted { get; set; } = true;

    [ObservableProperty]
    public partial bool NotifyQueueDone { get; set; } = true;

    [ObservableProperty]
    public partial int HistoryRetentionDays { get; set; }

    [ObservableProperty]
    public partial bool MinimizeToTray { get; set; }

    [ObservableProperty]
    public partial bool LoudnormEnabled { get; set; }

    [ObservableProperty]
    public partial int ConnectionsPerDownload { get; set; } = 3;

    [ObservableProperty]
    public partial bool RemoveEmojiFromNames { get; set; }

    [ObservableProperty]
    public partial bool PreferHigherBitrate { get; set; } = true;

    [ObservableProperty]
    public partial string YearTagSource { get; set; } = "Upload";

    [ObservableProperty]
    public partial string? DefaultAlbumArtist { get; set; }

    [ObservableProperty]
    public partial bool WriteDescriptionAsComment { get; set; }

    [ObservableProperty]
    public partial bool WriteArtwork { get; set; } = true;

    [ObservableProperty]
    public partial bool PlaylistAlbumTags { get; set; } = true;

    [ObservableProperty]
    public partial bool UseChannelAsArtist { get; set; } = true;

    [ObservableProperty]
    public partial bool CleanTagQuotesEmoji { get; set; }

    [ObservableProperty]
    public partial bool PauseOnMetered { get; set; } = true;

    [ObservableProperty]
    public partial bool PreventSleepWhileDownloading { get; set; }

    [ObservableProperty]
    public partial bool PlayOnComplete { get; set; }

    [ObservableProperty]
    public partial int MaxQueueSize { get; set; }

    [ObservableProperty]
    public partial string? BackupFolder { get; set; }

    [ObservableProperty]
    public partial bool SponsorBlockEnabled { get; set; }

    [ObservableProperty]
    public partial bool SaveSubtitlesAsSrt { get; set; }

    [ObservableProperty]
    public partial bool CompactCards { get; set; }

    [ObservableProperty]
    public partial string LastQueueFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string? LastQueueSearch { get; set; }

    [ObservableProperty]
    public partial double WindowWidth { get; set; }

    [ObservableProperty]
    public partial double WindowHeight { get; set; }

    [ObservableProperty]
    public partial double WindowX { get; set; }

    [ObservableProperty]
    public partial double WindowY { get; set; }

    [ObservableProperty]
    public partial long BytesDownloadedToday { get; set; }

    [ObservableProperty]
    public partial long BytesDownloadedTotal { get; set; }

    [ObservableProperty]
    public partial string? BytesDownloadedDay { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ChannelFormatMemoryEntry> ChannelFormatMemory { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<string> QueryHistory { get; set; } = [];

    [ObservableProperty]
    public partial string? PersistedQueueJson { get; set; }

    [JsonIgnore]
    public string? PendingQuery { get; set; }

    [ObservableProperty]
    public partial long SpeedLimitBytesPerSecond { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<DownloadHistoryEntry> DownloadHistory { get; set; } = [];

    partial void OnSpeedLimitBytesPerSecondChanged(long value) =>
        SpeedLimiter.Shared.SetBytesPerSecond(value);

    public void ApplySpeedLimit() =>
        SpeedLimiter.Shared.SetBytesPerSecond(SpeedLimitBytesPerSecond);

    partial void OnIsProxyEnabledChanged(bool value) => ApplyProxy();

    partial void OnProxyProtocolChanged(ProxyProtocol value) => ApplyProxy();

    partial void OnProxyHostChanged(string? value) => ApplyProxy();

    partial void OnProxyPortChanged(int value) => ApplyProxy();

    partial void OnProxyUsernameChanged(string? value) => ApplyProxy();

    partial void OnProxyPasswordChanged(string? value) => ApplyProxy();

    public void ApplyProxy() =>
        Http.ApplyProxy(
            new ProxyConfiguration(
                IsProxyEnabled,
                ProxyProtocol,
                ProxyHost,
                ProxyPort,
                ProxyUsername,
                ProxyPassword
            )
        );

    [ObservableProperty]
    [JsonConverter(typeof(AuthCookiesEncryptionConverter))]
    public partial IReadOnlyList<Cookie>? LastAuthCookies { get; set; }

    [ObservableProperty]
    [JsonConverter(typeof(ContainerConverter))]
    public partial Container LastContainer { get; set; } = Container.Mp4;

    [ObservableProperty]
    public partial VideoQualityPreference LastVideoQualityPreference { get; set; } =
        VideoQualityPreference.Highest;

    public override void Save()
    {
        // Clear the cookies if they are not supposed to be persisted
        var lastAuthCookies = LastAuthCookies;
        if (!IsAuthPersisted)
            LastAuthCookies = null;

        base.Save();

        LastAuthCookies = lastAuthCookies;
    }
}

public sealed record ChannelFormatMemoryEntry(
    string ChannelId,
    bool IsAudio,
    string Container,
    string? QualityOrBitrate
);

public partial class SettingsService
{
    [JsonSerializable(typeof(SettingsService))]
    [JsonSourceGenerationOptions(UseStringEnumConverter = true)]
    private partial class SettingsSerializerContext : JsonSerializerContext;
}
