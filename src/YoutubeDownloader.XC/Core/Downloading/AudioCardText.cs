using System;

namespace YoutubeDownloader.XC.Core.Downloading;

public static class AudioCardText
{
    public const string Empty = "<empty>";

    public static string Artist(string? channel) =>
        string.IsNullOrWhiteSpace(channel) ? Empty : channel.Trim();

    public static string Genre() => Empty;

    public static string Date(DateTimeOffset? uploadDate) =>
        uploadDate?.ToString("yyyy-MM-dd") ?? Empty;
}
