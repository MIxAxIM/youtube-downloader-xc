using System;
using System.Collections.Generic;
using System.Linq;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.Core.Downloading;

public sealed record MediaFormat(
    string Name,
    string FfmpegMuxer,
    bool IsAudioOnly,
    string? AudioCodec,
    bool IgnoresBitrate
);

public static class MediaFormats
{
    public static IReadOnlyList<MediaFormat> Video { get; } =
    [
        new("mp4", "mp4", false, null, false),
        new("webm", "webm", false, null, false),
        new("mkv", "matroska", false, null, false),
        new("mov", "mov", false, null, false),
        new("avi", "avi", false, null, false),
    ];

    public static IReadOnlyList<MediaFormat> Audio { get; } =
    [
        new("mp3", "mp3", true, "libmp3lame", false),
        new("m4a", "mp4", true, "aac", false),
        new("aac", "adts", true, "aac", false),
        new("ogg", "ogg", true, "libvorbis", false),
        new("opus", "opus", true, "libopus", false),
        new("flac", "flac", true, "flac", true),
        new("wav", "wav", true, "pcm_s16le", true),
        new("webm", "webm", true, "libopus", false),
        new("mp4", "mp4", true, "aac", false),
    ];

    public static IReadOnlyList<Container> Containers(bool audioOnly) =>
        (audioOnly ? Audio : Video).Select(format => new Container(format.Name)).ToArray();

    public static MediaFormat? Find(string name, bool audioOnly) =>
        (audioOnly ? Audio : Video).FirstOrDefault(format =>
            string.Equals(format.Name, name, StringComparison.OrdinalIgnoreCase)
        );

    public static Container ToFfmpegContainer(Container container, bool audioOnly)
    {
        var format = Find(container.Name, audioOnly);
        return format is null ? container : new Container(format.FfmpegMuxer);
    }

    public static bool RequiresCustomAudioMux(Container container) =>
        Find(container.Name, audioOnly: true) is { } format
        && !string.Equals(format.Name, format.FfmpegMuxer, StringComparison.OrdinalIgnoreCase);
}
