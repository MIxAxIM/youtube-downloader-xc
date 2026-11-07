using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using PowerKit;
using PowerKit.Extensions;
using YoutubeDownloader.Services;

namespace YoutubeDownloader.Localization;

public partial class LocalizationManager : ObservableObject, IDisposable
{
    private readonly IDisposable _eventSubscription;

    public LocalizationManager(SettingsService settingsService)
    {
        _eventSubscription = Disposable.Merge(
            settingsService.WatchProperty(o => o.Language, v => Language = v, true),
            this.WatchProperty(
                o => o.Language,
                _ =>
                {
                    foreach (var propertyName in EnglishLocalization.Keys)
                        OnPropertyChanged(propertyName);
                }
            )
        );
    }

    [ObservableProperty]
    public partial Language Language { get; set; } = Language.System;

    private string Get([CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var localization = Language switch
        {
            Language.System =>
                CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName.ToLowerInvariant() switch
                {
                    "ukr" => UkrainianLocalization,
                    "deu" => GermanLocalization,
                    "fra" => FrenchLocalization,
                    "spa" => SpanishLocalization,
                    "hun" => HungarianLocalization,
                    "kor" => KoreanLocalization,
                    "zho"
                        when CultureInfo
                            .CurrentUICulture.GetSelfAndParents()
                            .Any(c =>
                                string.Equals(c.Name, "zh-Hans", StringComparison.OrdinalIgnoreCase)
                            ) => ChineseSimplifiedLocalization,
                    _ => EnglishLocalization,
                },
            Language.Ukrainian => UkrainianLocalization,
            Language.German => GermanLocalization,
            Language.French => FrenchLocalization,
            Language.Spanish => SpanishLocalization,
            Language.ChineseSimplified => ChineseSimplifiedLocalization,
            Language.Hungarian => HungarianLocalization,
            Language.Korean => KoreanLocalization,
            _ => EnglishLocalization,
        };

        if (
            localization.TryGetValue(key, out var value)
            // English is used as a fallback
            || EnglishLocalization.TryGetValue(key, out value)
        )
        {
            return value;
        }

        return $"Missing localization for '{key}'";
    }

    public void Dispose() => _eventSubscription.Dispose();
}

public partial class LocalizationManager
{
    // ---- Dashboard ----

    public string QueryPlaceholderText => Get();
    public string QueryTooltip => Get();
    public string ProcessQueryTooltip => Get();
    public string AuthTooltip => Get();
    public string SettingsTooltip => Get();
    public string DashboardPlaceholder => Get();
    public string DownloadsFileColumnHeader => Get();
    public string DownloadsStatusColumnHeader => Get();
    public string ContextMenuRemoveSuccessful => Get();
    public string ContextMenuRemoveInactive => Get();
    public string ContextMenuRestartFailed => Get();
    public string ContextMenuCancelAll => Get();
    public string DownloadStatusEnqueued => Get();
    public string DownloadStatusCompleted => Get();
    public string DownloadStatusCanceled => Get();
    public string DownloadStatusFailed => Get();
    public string ClickToCopyErrorTooltip => Get();
    public string ShowFileTooltip => Get();
    public string PlayTooltip => Get();
    public string CancelDownloadTooltip => Get();
    public string RestartDownloadTooltip => Get();

    // ---- Settings ----

    public string SettingsTitle => Get();
    public string ThemeLabel => Get();
    public string ThemeTooltip => Get();
    public string LanguageLabel => Get();
    public string LanguageTooltip => Get();
    public string AutoUpdateLabel => Get();
    public string AutoUpdateTooltip => Get();
    public string PersistAuthLabel => Get();
    public string PersistAuthTooltip => Get();
    public string InjectAltLanguagesLabel => Get();
    public string InjectAltLanguagesTooltip => Get();
    public string InjectSubtitlesLabel => Get();
    public string InjectSubtitlesTooltip => Get();
    public string InjectTagsLabel => Get();
    public string InjectTagsTooltip => Get();
    public string SkipExistingFilesLabel => Get();
    public string SkipExistingFilesTooltip => Get();
    public string FileNameTemplateLabel => Get();
    public string FileNameTemplateTooltip => Get();
    public string ParallelLimitLabel => Get();
    public string ParallelLimitTooltip => Get();
    public string SettingsTabGeneral => Get();
    public string SettingsTabDownload => Get();
    public string SettingsTabNetwork => Get();
    public string SettingsTabAudio => Get();
    public string SettingsTabVideo => Get();
    public string SettingsTabTags => Get();
    public string SettingsTabAuthorization => Get();
    public string OkButton => Get();
    public string ApplyButton => Get();
    public string RestoreDefaultsButton => Get();
    public string VideoFolderLabel => Get();
    public string AudioFolderLabel => Get();
    public string SameFolderAsAudioLabel => Get();
    public string PlaylistSubfolderLabel => Get();
    public string AddCardsOnTopLabel => Get();
    public string AutoStartReadyLabel => Get();
    public string AutoRemoveCompletedLabel => Get();
    public string AutoExpandPlaylistLabel => Get();
    public string AutoAddClipboardLabel => Get();
    public string MinimizeToTrayLabel => Get();
    public string LoudnormLabel => Get();
    public string ProxyTestButton => Get();
    public string ProxyTestingLabel => Get();
    public string ProxyTestSuccessLabel => Get();
    public string ProxyTestFailedLabel => Get();
    public string ConnectionsPerDownloadLabel => Get();
    public string CompactLabel => Get();
    public string TitleLabel => Get();
    public string ArtistLabel => Get();
    public string AlbumLabel => Get();
    public string YearLabel => Get();
    public string OpenFolderLabel => Get();
    public string CopyLinkLabel => Get();
    public string SaveAudioLabel => Get();
    public string DownloadAgainLabel => Get();
    public string PauseLabel => Get();
    public string ResumeLabel => Get();
    public string SubtitlesLabel => Get();
    public string UseProxyLabel => Get();
    public string UseProxyTooltip => Get();
    public string ProxyProtocolLabel => Get();
    public string ProxyHostLabel => Get();
    public string ProxyPortLabel => Get();
    public string ProxyUsernameLabel => Get();
    public string ProxyPasswordLabel => Get();
    public string FFmpegPathLabel => Get();
    public string FFmpegPathTooltip => Get();
    public string FFmpegPathPlaceholderText => Get();
    public string FFmpegPathResetTooltip => Get();
    public string FFmpegPathBrowseTooltip => Get();

    // ---- Auth Setup ----

    public string AuthenticationTitle => Get();
    public string AuthenticatedText => Get();
    public string LogOutButton => Get();
    public string AuthenticationPlaceholderText => Get();

    // ---- Download Single Setup ----

    public string CopyMenuItem => Get();
    public string LiveLabel => Get();
    public string AudioLabel => Get();
    public string UpscaledLabel => Get();
    public string FormatLabel => Get();
    public string VideoLabel => Get();
    public string AudioBitrateLabel => Get();
    public string PasteButton => Get();
    public string PasteTooltip => Get();
    public string DownloadAllButton => Get();
    public string WaitingLabel => Get();
    public string DownloadedProgress => Get();
    public string UnsupportedBitrateTooltip => Get();
    public string GenreLabel => Get();
    public string DateLabel => Get();
    public string SpeedLimitLabel => Get();
    public string UnlimitedLabel => Get();
    public string HistoryTooltip => Get();
    public string HistoryTitle => Get();
    public string ClearHistoryButton => Get();
    public string HelpTooltip => Get();
    public string HelpTitle => Get();
    public string HelpMessage => Get();
    public string ClipboardEmptyMessage => Get();

    // ---- Download Multiple Setup ----

    public string ContainerLabel => Get();
    public string VideoQualityLabel => Get();

    // ---- Common buttons ----

    public string CloseButton => Get();
    public string DownloadButton => Get();
    public string CancelButton => Get();

    // ---- Dialog messages ----

    public string UkraineSupportTitle => Get();
    public string UkraineSupportMessage => Get();
    public string LearnMoreButton => Get();
    public string UnstableBuildTitle => Get();
    public string UnstableBuildMessage => Get();
    public string SeeReleasesButton => Get();
    public string FFmpegMissingTitle => Get();
    public string FFmpegMissingMessage => Get();
    public string FFmpegDownloadingTitle => Get();
    public string FFmpegDownloadCompletedTitle => Get();
    public string NothingFoundTitle => Get();
    public string NothingFoundMessage => Get();
    public string ErrorTitle => Get();
    public string UpdateDownloadingMessage => Get();
    public string UpdateReadyMessage => Get();
    public string UpdateInstallNowButton => Get();
    public string UpdateFailedMessage => Get();
}
