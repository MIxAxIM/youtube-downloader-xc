using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace YoutubeDownloader.XC.Core.Downloading;

public static class QueuePolicy
{
    public static double ProgressFill(bool completed, bool started, double fraction) =>
        completed ? 0
        : started ? Math.Clamp(fraction, 0, 1)
        : 0;

    public static string? CanonicalVideoId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var text = input.Trim();
        var query = text.IndexOf("?v=", StringComparison.OrdinalIgnoreCase);
        if (query >= 0)
            return TakeId(text[(query + 3)..]);

        foreach (var marker in new[] { "/shorts/", "/embed/", "/live/", "/v/" })
        {
            var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
                return TakeId(text[(index + marker.Length)..]);
        }

        var shortHost = text.IndexOf("youtu.be/", StringComparison.OrdinalIgnoreCase);
        if (shortHost >= 0)
            return TakeId(text[(shortHost + "youtu.be/".Length)..]);

        return TakeId(text);
    }

    public static bool IsDuplicate(IEnumerable<string?> existingIds, string? candidate)
    {
        var id = CanonicalVideoId(candidate);
        if (id is null)
            return false;

        return existingIds.Select(CanonicalVideoId).Any(existing => existing == id);
    }

    public static TimeSpan? TimeRemaining(
        long totalBytes,
        long downloadedBytes,
        double bytesPerSecond
    )
    {
        if (bytesPerSecond <= 0 || totalBytes <= downloadedBytes)
            return null;

        return TimeSpan.FromSeconds((totalBytes - downloadedBytes) / bytesPerSecond);
    }

    public static string FileNameFromTitle(string title, string container)
    {
        var cleaned = string.Join("_", title.Split(Path.GetInvalidFileNameChars())).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "video";

        return cleaned + "." + container.TrimStart('.');
    }

    public static string OutputFolder(
        bool audio,
        string? videoFolder,
        string? audioFolder,
        bool videoUsesAudioFolder,
        string? playlistTitle,
        bool placeInPlaylistSubfolder
    )
    {
        var root = audio || videoUsesAudioFolder ? audioFolder : videoFolder;
        root = string.IsNullOrWhiteSpace(root) ? "" : root;
        if (!placeInPlaylistSubfolder || string.IsNullOrWhiteSpace(playlistTitle))
            return root;

        return Path.Combine(root, FileNameFromTitle(playlistTitle, "").TrimEnd('.'));
    }

    public static bool IsCardVisible(
        string? title,
        string? search,
        string filter,
        bool isAudio,
        string status
    )
    {
        if (
            !string.IsNullOrWhiteSpace(search)
            && (title ?? "").Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) == false
        )
            return false;

        return filter switch
        {
            "Video" => !isAudio,
            "Audio" => isAudio,
            "Downloading" => status is "Started" or "Enqueued",
            "Completed" => status == "Completed",
            "Failed" => status == "Failed",
            _ => true,
        };
    }

    public static int ClampSimultaneous(int value) => Math.Clamp(value, 1, 20);

    public static bool ShouldApplyProxy(bool enabled, string? host, int port) =>
        enabled && !string.IsNullOrWhiteSpace(host) && port is >= 1 and <= 65535;

    public static bool IsProxyTestSuccess(int statusCode) => statusCode == 204;

    public static string RemoveEmoji(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value is >= 0x1F000 and <= 0x1FAFF or >= 0x2600 and <= 0x27BF)
                continue;

            builder.Append(rune.ToString());
        }

        return builder.ToString().Trim();
    }

    public static bool ShouldSkipExisting(bool enabled, bool fileExists) => enabled && fileExists;

    public static string ResolveArtist(
        string? typed,
        string? musicBrainz,
        string? channel,
        bool useChannelIfEmpty
    )
    {
        if (!string.IsNullOrWhiteSpace(typed))
            return typed.Trim();

        if (!string.IsNullOrWhiteSpace(musicBrainz))
            return musicBrainz.Trim();

        if (useChannelIfEmpty && !string.IsNullOrWhiteSpace(channel))
            return channel.Trim();

        return "";
    }

    public static int? ResolveYear(string source, int? uploadYear, int? musicBrainzYear) =>
        source switch
        {
            "MusicBrainz" => musicBrainzYear ?? uploadYear,
            "None" => null,
            _ => uploadYear,
        };

    public static bool ShouldWriteDescription(bool enabled) => enabled;

    public static IReadOnlyList<T> FilterHistoryByAge<T>(
        IReadOnlyList<T> entries,
        Func<T, DateTimeOffset> completedAt,
        string filter,
        DateTimeOffset now
    )
    {
        var cutoff = filter switch
        {
            "Today" => now.Date,
            "7Days" => now.AddDays(-7),
            "30Days" => now.AddDays(-30),
            _ => DateTimeOffset.MinValue,
        };

        return entries.Where(entry => completedAt(entry) >= cutoff).ToArray();
    }

    public static (TimeSpan? Start, TimeSpan? End) ParseClip(string? start, string? end) =>
        (ParseTime(start), ParseTime(end));

    public static long EstimateSize(IEnumerable<long> streamSizes) =>
        streamSizes.Where(size => size > 0).Sum();

    public static bool KeepsAudio(bool videoOnly) => !videoOnly;

    public static bool ShouldWriteSrt(bool enabled) => enabled;

    public static bool IsMembersOrPremiere(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains("members-only", StringComparison.OrdinalIgnoreCase)
            || message.Contains("premiere", StringComparison.OrdinalIgnoreCase)
            || message.Contains("not been premiered", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldStartDownload(bool networkIsMetered, bool pauseOnMetered) =>
        !(networkIsMetered && pauseOnMetered);

    public static string SettingsFilePath(
        bool portable,
        string applicationDirectory,
        string profileFile
    ) => portable ? Path.Combine(applicationDirectory, "Settings.json") : profileFile;

    public static bool IsValidCompletedFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            return new FileInfo(path).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static int InsertIndex(bool addOnTop, int count) => addOnTop ? 0 : count;

    public static bool CanAutoRetry(int attempt, bool canceled) => !canceled && attempt < 2;

    public static bool HasEnoughDiskSpace(
        long fileBytes,
        long freeBytes,
        long reserveBytes = 200_000_000
    ) => freeBytes - Math.Max(0, fileBytes) >= reserveBytes;

    public static IReadOnlyList<T> Move<T>(IReadOnlyList<T> items, int from, int to)
    {
        var list = items.ToList();
        if (from < 0 || to < 0 || from >= list.Count || to >= list.Count || from == to)
            return list;

        var item = list[from];
        list.RemoveAt(from);
        list.Insert(to, item);
        return list;
    }

    private static string? TakeId(string value)
    {
        var id = new string(
            value.TakeWhile(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-').ToArray()
        );
        return id.Length == 11 ? id : null;
    }

    private static TimeSpan? ParseTime(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (
            TimeSpan.TryParseExact(
                text.Trim(),
                [@"m\:ss", @"h\:mm\:ss", @"mm\:ss"],
                CultureInfo.InvariantCulture,
                out var time
            ) || TimeSpan.TryParse(text.Trim(), CultureInfo.InvariantCulture, out time)
        )
            return time;

        return null;
    }
}
