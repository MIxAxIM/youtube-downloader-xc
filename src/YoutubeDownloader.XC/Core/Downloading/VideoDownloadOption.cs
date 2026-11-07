using System;
using System.Collections.Generic;
using System.Linq;
using PowerKit.Extensions;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.XC.Core.Downloading;

public partial record VideoDownloadOption(
    Container Container,
    bool IsAudioOnly,
    IReadOnlyList<IStreamInfo> StreamInfos
)
{
    public VideoQuality? VideoQuality { get; } =
        StreamInfos.OfType<IVideoStreamInfo>().MaxBy(s => s.VideoQuality)?.VideoQuality;

    public bool IsVideoUpscaled { get; } =
        StreamInfos.OfType<IVideoStreamInfo>().Any(s => s.IsVideoUpscaled);

    public Bitrate? SourceAudioBitrate =>
        IsAudioOnly ? StreamInfos.OfType<IAudioStreamInfo>().MaxBy(s => s.Bitrate)?.Bitrate : null;

    public string? AudioBitrateLabel =>
        SourceAudioBitrate is { } bitrate ? $"{Math.Round(bitrate.KiloBitsPerSecond)} kbps" : null;
}

public partial record VideoDownloadOption
{
    internal static IReadOnlyList<VideoDownloadOption> ResolveAll(
        StreamManifest manifest,
        bool includeLanguageSpecificAudioStreams = true
    )
    {
        IEnumerable<VideoDownloadOption> GetVideoAndAudioOptions()
        {
            var videoStreamInfos = manifest
                .GetVideoStreams()
                .OrderByDescending(v => v.VideoQuality);

            foreach (var videoStreamInfo in videoStreamInfos)
            {
                // Muxed stream
                if (videoStreamInfo is MuxedStreamInfo)
                {
                    yield return new VideoDownloadOption(
                        videoStreamInfo.Container,
                        false,
                        [videoStreamInfo]
                    );
                }
                // Separate audio + video stream
                else
                {
                    var audioStreamInfos = manifest
                        .GetAudioStreams()
                        // Prefer audio streams with the same container
                        .OrderByDescending(s => s.Container == videoStreamInfo.Container)
                        .ThenByDescending(s => s is AudioOnlyStreamInfo)
                        .ThenByDescending(s => s.Bitrate)
                        .ToArray();

                    // Prefer language-specific audio streams, if available and if allowed
                    var languageSpecificAudioStreamInfos = includeLanguageSpecificAudioStreams
                        ? audioStreamInfos
                            .Where(s => s.AudioLanguage is not null)
                            .DistinctBy(s => s.AudioLanguage)
                            // Default language first so it's encoded as the first audio track in the output file
                            .OrderByDescending(s => s.IsAudioLanguageDefault)
                            .ToArray()
                        : [];

                    // If there are language-specific streams, include them all
                    if (languageSpecificAudioStreamInfos.Any())
                    {
                        yield return new VideoDownloadOption(
                            videoStreamInfo.Container,
                            false,
                            [videoStreamInfo, .. languageSpecificAudioStreamInfos]
                        );
                    }
                    // If there are no language-specific streams, download the single best quality audio stream
                    else
                    {
                        var audioStreamInfo = audioStreamInfos
                            // Prefer audio streams in the default language (or non-language-specific streams)
                            .OrderByDescending(s => s.IsAudioLanguageDefault ?? true)
                            .FirstOrDefault();

                        if (audioStreamInfo is not null)
                        {
                            yield return new VideoDownloadOption(
                                videoStreamInfo.Container,
                                false,
                                [videoStreamInfo, audioStreamInfo]
                            );
                        }
                    }
                }
            }
        }

        IEnumerable<VideoDownloadOption> GetAudioOnlyOptions()
        {
            var audioStreams = manifest.GetAudioStreams().ToArray();
            var audioOnlyStreams = audioStreams.Where(s => s is AudioOnlyStreamInfo).ToArray();
            if (audioOnlyStreams.Length > 0)
                audioStreams = audioOnlyStreams;

            var preferredStreams = audioStreams
                .Where(s => s.IsAudioLanguageDefault ?? true)
                .ToArray();
            if (preferredStreams.Length == 0)
                preferredStreams = audioStreams;

            foreach (var stream in preferredStreams.OrderByDescending(s => s.Bitrate))
                yield return new VideoDownloadOption(stream.Container, true, [stream]);

            var transcodeSource = preferredStreams
                .OrderByDescending(s => s.IsAudioLanguageDefault ?? true)
                .ThenByDescending(s => s.Container == Container.WebM)
                .ThenByDescending(s => s is AudioOnlyStreamInfo)
                .ThenByDescending(s => s.Bitrate)
                .FirstOrDefault();

            if (transcodeSource is not null)
            {
                yield return new VideoDownloadOption(Container.Mp3, true, [transcodeSource]);
                yield return new VideoDownloadOption(new Container("ogg"), true, [transcodeSource]);
            }

            var aacSource = preferredStreams
                .OrderByDescending(s => s.IsAudioLanguageDefault ?? true)
                .ThenByDescending(s => s.Container == Container.Mp4)
                .ThenByDescending(s => s is AudioOnlyStreamInfo)
                .ThenByDescending(s => s.Bitrate)
                .FirstOrDefault();

            if (aacSource is not null)
                yield return new VideoDownloadOption(new Container("m4a"), true, [aacSource]);
        }

        // Deduplicate download options. Audio rows with the same container stay distinct
        // when their source bitrates differ.
        var comparer = EqualityComparer<VideoDownloadOption>.Create(
            (x, y) =>
                x?.VideoQuality == y?.VideoQuality
                && x?.IsVideoUpscaled == y?.IsVideoUpscaled
                && x?.IsAudioOnly == y?.IsAudioOnly
                && x?.Container == y?.Container
                && x?.SourceAudioBitrate == y?.SourceAudioBitrate,
            x =>
                HashCode.Combine(
                    x.VideoQuality,
                    x.IsVideoUpscaled,
                    x.IsAudioOnly,
                    x.Container,
                    x.SourceAudioBitrate
                )
        );

        var options = new HashSet<VideoDownloadOption>(comparer);

        options.AddRange(GetVideoAndAudioOptions());
        options.AddRange(GetAudioOnlyOptions());

        return options.ToArray();
    }
}
