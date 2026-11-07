using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Gress;
using YoutubeDownloader.XC.Core.Utils;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.XC.Core.Downloading;

public class VideoDownloader(IReadOnlyList<Cookie>? initialCookies = null) : IDisposable
{
    private readonly YoutubeClient _youtube = new(Http.Client, initialCookies ?? []);

    public async Task<IReadOnlyList<VideoDownloadOption>> GetDownloadOptionsAsync(
        VideoId videoId,
        bool includeLanguageSpecificAudioStreams = true,
        CancellationToken cancellationToken = default
    )
    {
        var manifest = await _youtube.Videos.Streams.GetManifestAsync(videoId, cancellationToken);
        return VideoDownloadOption.ResolveAll(manifest, includeLanguageSpecificAudioStreams);
    }

    public async Task<VideoDownloadOption> GetBestDownloadOptionAsync(
        VideoId videoId,
        VideoDownloadPreference preference,
        bool includeLanguageSpecificAudioStreams = true,
        CancellationToken cancellationToken = default
    )
    {
        var options = await GetDownloadOptionsAsync(
            videoId,
            includeLanguageSpecificAudioStreams,
            cancellationToken
        );

        return preference.TryGetBestOption(options)
            ?? throw new InvalidOperationException("No suitable download option found.");
    }

    public async Task<IReadOnlyList<ClosedCaptionTrackInfo>> GetCaptionTracksAsync(
        VideoId videoId,
        CancellationToken cancellationToken = default
    )
    {
        var manifest = await _youtube.Videos.ClosedCaptions.GetManifestAsync(
            videoId,
            cancellationToken
        );
        return manifest.Tracks.ToArray();
    }

    public async Task DownloadVideoAsync(
        string filePath,
        IVideo video,
        VideoDownloadOption downloadOption,
        bool includeSubtitles = true,
        string? ffmpegPath = null,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default,
        AudioBitratePreference audioBitrate = AudioBitratePreference.Source,
        string? subtitleLanguage = null,
        bool applyLoudnorm = false,
        DownloadPauseGate? pauseGate = null
    )
    {
        var dirPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dirPath))
            Directory.CreateDirectory(dirPath);

        if (pauseGate is not null)
            await pauseGate.WaitIfPausedAsync(cancellationToken);

        if (
            downloadOption.IsAudioOnly
            && AudioEncoder.NeedsCustomMux(downloadOption.Container, audioBitrate)
        )
        {
            var audioStream =
                downloadOption.StreamInfos.OfType<IAudioStreamInfo>().FirstOrDefault()
                ?? throw new InvalidOperationException("No audio stream found.");

            await AudioEncoder.DownloadAsync(
                _youtube,
                filePath,
                audioStream,
                downloadOption.Container,
                audioBitrate,
                ffmpegPath ?? FFmpeg.TryGetCliFilePath() ?? "ffmpeg",
                progress,
                cancellationToken,
                applyLoudnorm,
                pauseGate
            );
            return;
        }

        var trackInfos = new List<ClosedCaptionTrackInfo>();
        if (!downloadOption.IsAudioOnly && includeSubtitles)
        {
            var manifest = await _youtube.Videos.ClosedCaptions.GetManifestAsync(
                video.Id,
                cancellationToken
            );

            if (string.IsNullOrWhiteSpace(subtitleLanguage) || subtitleLanguage == "all")
            {
                trackInfos.AddRange(manifest.Tracks);
            }
            else if (subtitleLanguage != "none")
            {
                trackInfos.AddRange(
                    manifest.Tracks.Where(track =>
                        string.Equals(
                            track.Language.Code,
                            subtitleLanguage,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                );
            }
        }

        if (pauseGate is not null)
            await pauseGate.WaitIfPausedAsync(cancellationToken);

        await _youtube.Videos.DownloadAsync(
            downloadOption.StreamInfos,
            trackInfos,
            new ConversionRequestBuilder(filePath)
                .SetFFmpegPath(ffmpegPath ?? FFmpeg.TryGetCliFilePath() ?? "ffmpeg")
                .SetContainer(
                    MediaFormats.ToFfmpegContainer(
                        downloadOption.Container,
                        downloadOption.IsAudioOnly
                    )
                )
                .SetPreset(ConversionPreset.Medium)
                .Build(),
            progress?.ToDoubleBased(),
            cancellationToken
        );
    }

    public Task ExtractAudioFromFileAsync(
        string sourceFilePath,
        string outputFilePath,
        Container container,
        string? ffmpegPath = null,
        CancellationToken cancellationToken = default,
        bool applyLoudnorm = false
    ) =>
        AudioEncoder.ExtractAudioAsync(
            sourceFilePath,
            outputFilePath,
            container,
            ffmpegPath ?? FFmpeg.TryGetCliFilePath() ?? "ffmpeg",
            cancellationToken,
            applyLoudnorm
        );

    public void Dispose() => _youtube.Dispose();
}
