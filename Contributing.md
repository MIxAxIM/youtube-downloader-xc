# Contributing

## Local development

Requirements:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Optional: [Dev Containers](https://containers.dev) for a reproducible environment

Open the solution:

```bash
dotnet restore YoutubeDownloader.XC.slnx
dotnet build YoutubeDownloader.XC.slnx -c Release
dotnet test YoutubeDownloader.XC.slnx -c Release
```

Publish a runnable Windows build:

```bash
dotnet publish src/YoutubeDownloader.XC/YoutubeDownloader.XC.csproj -c Release -r win-x64 --self-contained false -o artifacts/win-x64
```

## Dev Container (optional)

If you use the included Dev Container, a remote desktop tunnel may be available at [http://localhost:6080](http://localhost:6080). That tunnel is only for viewing the container desktop — it is not a separate web version of the app.

## Layout

```text
src/YoutubeDownloader.XC/
  Core/      download, resolve, tagging, FFmpeg helpers
  Desktop/   Avalonia UI, view models, settings, localization
  Auth/      WebView YouTube sign-in UI
tests/YoutubeDownloader.XC.Tests/
docs/
```

## Upstream

This repo keeps `upstream` pointed at [Tyrrrz/YoutubeDownloader](https://github.com/Tyrrrz/YoutubeDownloader). After the XC layout change, upstream updates are a manual port, not a clean merge.
