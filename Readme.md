# Youtube Downloader XC

Desktop app for downloading YouTube videos, playlists, and channels in the format you choose.

Based on [Tyrrrz/YoutubeDownloader](https://github.com/Tyrrrz/YoutubeDownloader).  
Copyright (C) Oleksii Holub — released under the [MIT License](License.txt).

## Features

- Paste a video, playlist, channel, or search query into the queue
- Download video or audio with selectable quality, format, and bitrate
- Optional full-playlist expand when a video URL also includes a playlist id
- Queue tools: pause/resume, history, tray, speed limit, simultaneous downloads
- Subtitles, media tags, SponsorBlock, proxy, and YouTube account sign-in
- Settings window with General, Download, Network, Audio, Video, Tags, and Authorization tabs

See the full feature list and settings reference in [`docs/`](docs/).

## Download / run

Published Windows builds go to:

```text
artifacts/win-x64/YoutubeDownloader.XC.exe
```

Build one yourself:

```bash
dotnet publish src/YoutubeDownloader.XC/YoutubeDownloader.XC.csproj -c Release -r win-x64 --self-contained false -o artifacts/win-x64
```

More detail: [docs/building.md](docs/building.md).

## Documentation

| Doc | Contents |
| --- | --- |
| [docs/features.md](docs/features.md) | What the app can do |
| [docs/settings.md](docs/settings.md) | Settings reference |
| [docs/queue-and-playlists.md](docs/queue-and-playlists.md) | Paste / playlist behavior |
| [docs/building.md](docs/building.md) | Build, test, and publish |

## Requirements

- .NET 10 runtime (or SDK to build from source)
- FFmpeg (the app can download it if missing)
- Windows WebView2 Runtime for YouTube sign-in

## Credits

Engine interaction with YouTube uses [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode).  
Upstream project: [Tyrrrz/YoutubeDownloader](https://github.com/Tyrrrz/YoutubeDownloader).
