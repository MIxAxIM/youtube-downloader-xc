# Changelog

All notable changes to Youtube Downloader XC are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Sign-in opens in its own window, with the YouTube login page, instead of inside the main window.
- Cards show an error row for a failed download, a blocked video, or any other error. When YouTube asks for a sign-in, the row includes a **Sign in** button and the sign-in window opens.
- Settings → Authorization shows a green check and the signed-in account email, handle, or name when YouTube provides it.
- The main-window auth button turns into a green account check after you sign in. Its tooltip shows the account name.

### Changed

- The button next to **Download all** is a vertical-dots menu. The gear icon is only for the Settings window.
- A video copied from a YouTube Mix (`list=RD...`) is added as that video only. The mix is not offered as a playlist import.
- Compact mode and the audio/video switch stay usable when a video is waiting for sign-in. Clicking a switch no longer selects and shifts the card underneath it.

### Fixed

- The sign-in message is shown once, in the error row.

## [1.1.0] - 2026-11-08

First Youtube Downloader XC release. The app is restructured under `src/YoutubeDownloader.XC` and `tests/YoutubeDownloader.XC.Tests`.

### Added

- Queue cards for video, playlist, channel, and search queries, with format, quality, clip times, subtitles, and progress.
- Compact cards still show the video quality choices.
- Audio mode with bitrate choices, loudness normalization, and artist/title file names instead of the pasted URL.
- Pasting a watch link that also contains a normal playlist adds only that video. A playlist icon on the card imports the rest.
- Settings in a separate window: theme, language, folders, proxy, tags, and authorization.
- Download history, tray pause/resume, speed limit, and queue persistence.
- CI builds for Windows, Linux, and macOS. The macOS bundle name stays within the 15-character limit (`YoutubeDLXC`).

[Unreleased]: https://github.com/MIxAxIM/youtube-downloader-xc/compare/1.1.0...HEAD
[1.1.0]: https://github.com/MIxAxIM/youtube-downloader-xc/releases/tag/1.1.0
