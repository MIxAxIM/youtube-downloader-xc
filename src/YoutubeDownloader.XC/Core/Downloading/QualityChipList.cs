using System;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeDownloader.XC.Core.Downloading;

public sealed record QualityChip(string Label, int Height, bool IsUpscaled, bool IsSelected);

public readonly record struct QualityOptionSnapshot(
    string Label,
    int Height,
    string Container,
    bool IsAudioOnly,
    bool IsUpscaled
);

public static class QualityChipList
{
    public static int? SelectHeight(IReadOnlyList<int> heights, int? current)
    {
        if (heights.Count == 0)
            return null;

        if (current is int height && heights.Contains(height))
            return height;

        return heights.Max();
    }

    public static IReadOnlyList<QualityChip> Build(
        IEnumerable<QualityOptionSnapshot> options,
        string? container,
        int? selectedHeight
    )
    {
        var matches = options
            .Where(option =>
                !option.IsAudioOnly
                && (
                    string.IsNullOrWhiteSpace(container)
                    || string.Equals(
                        option.Container,
                        container,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            .GroupBy(option => option.Height)
            .Select(group => group.OrderBy(option => option.IsUpscaled).First())
            .OrderByDescending(option => option.Height)
            .ToArray();

        var selected = SelectHeight(
            matches.Select(option => option.Height).ToArray(),
            selectedHeight
        );

        return matches
            .Select(option => new QualityChip(
                option.Label,
                option.Height,
                option.IsUpscaled,
                option.Height == selected
            ))
            .ToArray();
    }
}
