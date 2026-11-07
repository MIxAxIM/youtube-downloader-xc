# Building

## Prerequisites

- .NET 10 SDK
- On Windows, WebView2 Runtime for the sign-in dialog
- FFmpeg (optional at build time; the app can fetch it at runtime)

## Restore and build

```bash
dotnet restore YoutubeDownloader.XC.slnx
dotnet build YoutubeDownloader.XC.slnx -c Release
```

## Tests

```bash
dotnet test YoutubeDownloader.XC.slnx -c Release
```

## Publish (Windows x64)

```bash
dotnet publish src/YoutubeDownloader.XC/YoutubeDownloader.XC.csproj -c Release -r win-x64 --self-contained false -o artifacts/win-x64
```

Run:

```text
artifacts/win-x64/YoutubeDownloader.XC.exe
```

Project-local `bin/` and `obj/` folders are normal MSBuild outputs and are gitignored. Prefer `artifacts/win-x64` as the runnable output.

## Solution layout

| Path | Role |
| --- | --- |
| `src/YoutubeDownloader.XC` | Single app project |
| `src/YoutubeDownloader.XC/Core` | Engine (download, resolve, tags) |
| `src/YoutubeDownloader.XC/Desktop` | Avalonia UI shell |
| `src/YoutubeDownloader.XC/Auth` | WebView sign-in UI |
| `tests/YoutubeDownloader.XC.Tests` | Unit tests |

## Remotes

- `origin` — this fork (`youtube-downloader-xc`)
- `upstream` — [Tyrrrz/YoutubeDownloader](https://github.com/Tyrrrz/YoutubeDownloader) (manual ports after the XC restructure)
