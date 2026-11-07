using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDownloader.XC.Core.Utils;

namespace YoutubeDownloader.XC.Core.Downloading;

public static class SponsorBlockClient
{
    public sealed record Segment(double StartSeconds, double EndSeconds);

    public static async Task<IReadOnlyList<Segment>> GetSegmentsAsync(
        string videoId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var url =
                "https://sponsor.ajay.app/api/skipSegments?videoID="
                + Uri.EscapeDataString(videoId)
                + "&categories=[\"sponsor\",\"intro\",\"outro\"]";

            using var response = await Http.Client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken
            );
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var segments = new List<Segment>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("segment", out var range) || range.GetArrayLength() < 2)
                    continue;

                segments.Add(new Segment(range[0].GetDouble(), range[1].GetDouble()));
            }

            return segments.OrderBy(segment => segment.StartSeconds).ToArray();
        }
        catch
        {
            return [];
        }
    }

    public static string BuildCutFilter(IReadOnlyList<Segment> segments, double durationSeconds)
    {
        if (segments.Count == 0 || durationSeconds <= 0)
            return "";

        var keep = new List<(double Start, double End)>();
        var cursor = 0.0;
        foreach (var segment in segments)
        {
            var start = Math.Clamp(segment.StartSeconds, 0, durationSeconds);
            var end = Math.Clamp(segment.EndSeconds, 0, durationSeconds);
            if (end <= start || start < cursor)
                continue;

            if (start > cursor)
                keep.Add((cursor, start));
            cursor = Math.Max(cursor, end);
        }

        if (cursor < durationSeconds)
            keep.Add((cursor, durationSeconds));

        if (keep.Count == 0)
            return "";

        return string.Join(
            "+",
            keep.Select(
                (range, index) =>
                    $"[0:v]trim=start={range.Start:0.###}:end={range.End:0.###},setpts=PTS-STARTPTS[v{index}];"
                    + $"[0:a]atrim=start={range.Start:0.###}:end={range.End:0.###},asetpts=PTS-STARTPTS[a{index}]"
            )
        );
    }
}
