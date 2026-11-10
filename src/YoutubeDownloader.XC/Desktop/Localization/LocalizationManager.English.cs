using System.Collections.Generic;

namespace YoutubeDownloader.XC.Localization;

public partial class LocalizationManager
{
    private static readonly IReadOnlyDictionary<string, string> EnglishLocalization =
        new Dictionary<string, string>
        {
            // Dashboard
            [nameof(QueryPlaceholderText)] = "URL or search query",
            [nameof(QueryTooltip)] =
                "Any valid YouTube URL or ID is accepted. Prepend a question mark (?) to perform search by text.",
            [nameof(ProcessQueryTooltip)] = "Process query (Enter)",
            [nameof(AuthTooltip)] = "Authentication",
            [nameof(SettingsTooltip)] = "Settings",
            [nameof(DashboardPlaceholder)] = """
                Copy-paste a **URL** or enter a **search query** to start downloading
                Press **Shift+Enter** to add multiple items
                """,
            [nameof(DownloadsFileColumnHeader)] = "File",
            [nameof(DownloadsStatusColumnHeader)] = "Status",
            [nameof(ContextMenuRemoveSuccessful)] = "Remove successful downloads",
            [nameof(ContextMenuRemoveInactive)] = "Remove inactive downloads",
            [nameof(ContextMenuRestartFailed)] = "Restart failed downloads",
            [nameof(ContextMenuCancelAll)] = "Cancel all downloads",
            [nameof(DownloadStatusEnqueued)] = "Pending...",
            [nameof(DownloadStatusCompleted)] = "Done",
            [nameof(DownloadStatusCanceled)] = "Canceled",
            [nameof(DownloadStatusFailed)] = "Failed",
            [nameof(ClickToCopyErrorTooltip)] = "Note: Click to copy this error message",
            [nameof(ShowFileTooltip)] = "Show file",
            [nameof(PlayTooltip)] = "Play",
            [nameof(CancelDownloadTooltip)] = "Cancel download",
            [nameof(RestartDownloadTooltip)] = "Restart download",
            // Settings
            [nameof(SettingsTitle)] = "Settings",
            [nameof(ThemeLabel)] = "Theme",
            [nameof(ThemeTooltip)] = "Preferred user interface theme",
            [nameof(LanguageLabel)] = "Language",
            [nameof(LanguageTooltip)] = "Preferred display language for the user interface",
            [nameof(AutoUpdateLabel)] = "Auto-update",
            [nameof(AutoUpdateTooltip)] = """
                Perform automatic updates on every launch.
                **Warning:** it's recommended to leave this option enabled to ensure that the app is compatible with the latest version of YouTube.
                """,
            [nameof(PersistAuthLabel)] = "Persist authentication",
            [nameof(PersistAuthTooltip)] = """
                Save authentication cookies to a file so that they can be persisted between sessions.
                **Warning**: although the cookies are stored with encryption, they may still be recovered by an attacker who has access to your system.
                """,
            [nameof(InjectAltLanguagesLabel)] = "Inject alternative languages",
            [nameof(InjectAltLanguagesTooltip)] =
                "Inject audio tracks in alternative languages (if available) into downloaded files",
            [nameof(InjectSubtitlesLabel)] = "Inject subtitles",
            [nameof(InjectSubtitlesTooltip)] =
                "Inject subtitles (if available) into downloaded files",
            [nameof(InjectTagsLabel)] = "Inject media tags",
            [nameof(InjectTagsTooltip)] = "Inject media tags (if available) into downloaded files",
            [nameof(SkipExistingFilesLabel)] = "Skip existing files",
            [nameof(SkipExistingFilesTooltip)] =
                "When downloading multiple videos, skip those that already have matching files in the output directory",
            [nameof(FileNameTemplateLabel)] = "File name template",
            [nameof(FileNameTemplateTooltip)] = """
                Template used for generating file names for downloaded videos.

                Available tokens:
                **$num** — video's position in the list (if applicable)
                **$id** — video ID
                **$title** — video title
                **$author** — video author
                """,
            [nameof(ParallelLimitLabel)] = "Simultaneous downloads",
            [nameof(ParallelLimitTooltip)] =
                "How many downloads can be active at the same time (1-20)",
            [nameof(SettingsTabGeneral)] = "General",
            [nameof(SettingsTabDownload)] = "Download",
            [nameof(SettingsTabNetwork)] = "Network",
            [nameof(SettingsTabAudio)] = "Audio",
            [nameof(SettingsTabVideo)] = "Video",
            [nameof(SettingsTabTags)] = "Tags",
            [nameof(SettingsTabAuthorization)] = "Authorization",
            [nameof(OkButton)] = "OK",
            [nameof(ApplyButton)] = "Apply",
            [nameof(RestoreDefaultsButton)] = "Restore defaults",
            [nameof(VideoFolderLabel)] = "Video folder",
            [nameof(AudioFolderLabel)] = "Audio folder",
            [nameof(SameFolderAsAudioLabel)] = "Use audio folder for video",
            [nameof(PlaylistSubfolderLabel)] = "Playlist subfolder",
            [nameof(AddCardsOnTopLabel)] = "Add new cards on top",
            [nameof(AutoStartReadyLabel)] = "Start downloads when ready",
            [nameof(AutoRemoveCompletedLabel)] = "Remove completed cards",
            [nameof(AutoExpandPlaylistLabel)] =
                "Load full playlist when pasting a video with a playlist link",
            [nameof(AutoAddClipboardLabel)] = "Add copied YouTube links",
            [nameof(MinimizeToTrayLabel)] = "Minimize to tray",
            [nameof(LoudnormLabel)] = "Normalize loudness",
            [nameof(ProxyTestButton)] = "Test proxy",
            [nameof(ProxyTestingLabel)] = "Testing...",
            [nameof(ProxyTestSuccessLabel)] = "Proxy works",
            [nameof(ProxyTestFailedLabel)] = "Proxy failed",
            [nameof(ConnectionsPerDownloadLabel)] = "Connections per download",
            [nameof(CompactLabel)] = "Compact",
            [nameof(TitleLabel)] = "Title",
            [nameof(ArtistLabel)] = "Artist",
            [nameof(AlbumLabel)] = "Album",
            [nameof(YearLabel)] = "Year",
            [nameof(OpenFolderLabel)] = "Open folder",
            [nameof(CopyLinkLabel)] = "Copy link",
            [nameof(SaveAudioLabel)] = "Save audio",
            [nameof(DownloadAgainLabel)] = "Download again",
            [nameof(PauseLabel)] = "Pause",
            [nameof(ResumeLabel)] = "Resume",
            [nameof(SubtitlesLabel)] = "Subtitles",
            [nameof(UseProxyLabel)] = "Use proxy",
            [nameof(UseProxyTooltip)] =
                "Send YouTube requests, downloads, tagging, and updates through this proxy while it is enabled.",
            [nameof(ProxyProtocolLabel)] = "Protocol",
            [nameof(ProxyHostLabel)] = "Host",
            [nameof(ProxyPortLabel)] = "Port",
            [nameof(ProxyUsernameLabel)] = "Username",
            [nameof(ProxyPasswordLabel)] = "Password",
            [nameof(FFmpegPathLabel)] = "FFmpeg path",
            [nameof(FFmpegPathTooltip)] =
                "Path to the FFmpeg executable. Leave empty to use auto-detection.",
            [nameof(FFmpegPathPlaceholderText)] = "Auto-detect",
            [nameof(FFmpegPathResetTooltip)] = "Reset to auto-detection",
            [nameof(FFmpegPathBrowseTooltip)] = "Browse for FFmpeg executable",
            // Auth Setup
            [nameof(AuthenticationTitle)] = "Authentication",
            [nameof(AuthenticatedText)] = "You are currently authenticated",
            [nameof(LogOutButton)] = "Log out",
            [nameof(AuthenticationPlaceholderText)] = """
                Loading...

                If the browser does not appear:
                - Windows: install Microsoft Edge WebView2 Runtime.
                - macOS: install the latest system updates.
                - Linux: install GTK 3, WebKitGTK 4.1, and libsoup 3.
                """,
            // Download Single Setup
            [nameof(CopyMenuItem)] = "Copy",
            [nameof(LiveLabel)] = "Live",
            [nameof(AudioLabel)] = "Audio",
            [nameof(UpscaledLabel)] = "Upscaled",
            [nameof(FormatLabel)] = "Format",
            [nameof(VideoLabel)] = "Video",
            [nameof(AudioBitrateLabel)] = "Audio bitrate",
            [nameof(PasteButton)] = "Paste link",
            [nameof(PasteTooltip)] = "Paste a link from the clipboard and add it to the queue",
            [nameof(ImportPlaylistTooltip)] = "Import all videos from this playlist",
            [nameof(VideoBlockedMessage)] =
                "YouTube needs you to sign in before this video can be loaded.",
            [nameof(SignInButton)] = "Sign in",
            [nameof(DownloadAllButton)] = "Download all",
            [nameof(WaitingLabel)] = "Waiting...",
            [nameof(DownloadedProgress)] = "Downloaded {0} of {1}",
            [nameof(UnsupportedBitrateTooltip)] =
                "YouTube did not provide this bitrate. The app can still encode the file at this rate.",
            [nameof(GenreLabel)] = "Genre",
            [nameof(DateLabel)] = "Date",
            [nameof(SpeedLimitLabel)] = "Speed limit",
            [nameof(UnlimitedLabel)] = "Unlimited",
            [nameof(HistoryTooltip)] = "Download history",
            [nameof(HistoryTitle)] = "History",
            [nameof(ClearHistoryButton)] = "CLEAR",
            [nameof(HelpTooltip)] = "Help",
            [nameof(HelpTitle)] = "Help",
            [nameof(HelpMessage)] = """
                Paste a link to add videos to the queue. Nothing downloads until you press a download button.

                The top bar sets the format and bitrate for cards that have not started. Each card can change its own quality, and audio cards can change bitrate.

                A muted bitrate with an information icon was not provided by YouTube. The app can still encode it.

                The card fills from the left while it downloads.
                """,
            [nameof(ClipboardEmptyMessage)] = "The clipboard does not contain a link",
            // Download Multiple Setup
            [nameof(ContainerLabel)] = "Container",
            [nameof(VideoQualityLabel)] = "Video quality",
            // Common buttons
            [nameof(CloseButton)] = "CLOSE",
            [nameof(DownloadButton)] = "DOWNLOAD",
            [nameof(CancelButton)] = "Cancel",
            // Dialog messages
            [nameof(UnstableBuildTitle)] = "Unstable build warning",
            [nameof(UnstableBuildMessage)] = """
                You're using a development build of {0}. These builds are not thoroughly tested and may contain bugs.

                Auto-updates are disabled for development builds.

                Click SEE RELEASES if you want to download a stable release instead.
                """,
            [nameof(SeeReleasesButton)] = "SEE RELEASES",
            [nameof(FFmpegMissingTitle)] = "FFmpeg is missing",
            [nameof(FFmpegMissingMessage)] = """
                FFmpeg was not found on your system. It is required for {0} to work. Would you like to download it now?

                Alternatively, you can close this dialog and manually set a custom FFmpeg path in settings.
                """,
            [nameof(FFmpegDownloadingTitle)] = "Downloading FFmpeg...",
            [nameof(FFmpegDownloadCompletedTitle)] = "FFmpeg downloaded",
            [nameof(NothingFoundTitle)] = "Nothing found",
            [nameof(NothingFoundMessage)] =
                "Couldn't find any videos based on the query or URL you provided",
            [nameof(ErrorTitle)] = "Error",
            [nameof(UpdateDownloadingMessage)] = "Downloading update to {0} v{1}...",
            [nameof(UpdateReadyMessage)] =
                "Update has been downloaded and will be installed when you exit",
            [nameof(UpdateInstallNowButton)] = "INSTALL NOW",
            [nameof(UpdateFailedMessage)] = "Failed to perform application update",
        };
}
