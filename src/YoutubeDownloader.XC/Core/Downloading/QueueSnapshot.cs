using System;
using System.Collections.Generic;
using System.Text.Json;

namespace YoutubeDownloader.XC.Core.Downloading;

public sealed record QueueCardSnapshot(
    string? SourceQuery,
    string? VideoId,
    string? VideoUrl,
    string? Title,
    string? Artist,
    string? Year,
    string? Album,
    string Status,
    bool IsAudio,
    string Container,
    string? PlaylistTitle,
    string? FilePath,
    string SubtitleChoice,
    string? ClipStart,
    string? ClipEnd,
    string? CoverTime,
    bool VideoOnly,
    bool SaveSrt
);

public static class QueueSnapshot
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public static string Serialize(IEnumerable<QueueCardSnapshot> cards) =>
        JsonSerializer.Serialize(cards, Options);

    public static IReadOnlyList<QueueCardSnapshot> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<QueueCardSnapshot>>(json, Options) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string NormalizeStatus(string status) =>
        status is "Started" or "Enqueued" ? "Ready" : status;
}
