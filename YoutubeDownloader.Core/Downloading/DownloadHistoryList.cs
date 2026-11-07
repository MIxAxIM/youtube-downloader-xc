using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YoutubeDownloader.Core.Downloading;

public sealed record DownloadHistoryEntry(
    string Title,
    string FilePath,
    DateTimeOffset CompletedAt,
    string? Url = null,
    TimeSpan? Duration = null
);

public static class DownloadHistoryList
{
    public const int Cap = 200;

    public static IReadOnlyList<DownloadHistoryEntry> Append(
        IReadOnlyList<DownloadHistoryEntry>? history,
        DownloadHistoryEntry entry
    )
    {
        var next = new List<DownloadHistoryEntry> { entry };
        if (history is not null)
            next.AddRange(history);

        if (next.Count > Cap)
            next.RemoveRange(Cap, next.Count - Cap);

        return next;
    }

    public static IReadOnlyList<DownloadHistoryEntry> Clear() => [];

    public static IReadOnlyList<DownloadHistoryEntry> Delete(
        IReadOnlyList<DownloadHistoryEntry> history,
        DownloadHistoryEntry entry
    ) => history.Where(item => item != entry).ToArray();

    public static string ExportVisible(IEnumerable<DownloadHistoryEntry> entries)
    {
        var builder = new StringBuilder();
        foreach (var entry in entries)
        {
            builder.Append(entry.CompletedAt.ToString("u"));
            builder.Append('\t');
            builder.Append(entry.Title);
            builder.Append('\t');
            builder.Append(entry.Url);
            builder.Append('\t');
            builder.Append(entry.FilePath);
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
