using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace YoutubeDownloader.Core.Tagging;

public static partial class ChapterList
{
    [GeneratedRegex(@"^(?<time>\d{1,2}:\d{2}(?::\d{2})?)\s+(?<title>.+)$", RegexOptions.Multiline)]
    private static partial Regex ChapterLineRegex();

    public static IReadOnlyList<string> FromDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return [];

        var chapters = new List<string>();
        foreach (Match match in ChapterLineRegex().Matches(description))
            chapters.Add($"{match.Groups["time"].Value} {match.Groups["title"].Value.Trim()}");

        return chapters;
    }
}
