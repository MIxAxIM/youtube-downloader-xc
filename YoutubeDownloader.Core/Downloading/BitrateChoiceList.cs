using System;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeDownloader.Core.Downloading;

public sealed record BitrateChoice(int? KilobitsPerSecond, bool IsNative, string Label)
{
    public static BitrateChoice Source { get; } = new(null, true, "Source");
}

public static class BitrateChoiceList
{
    public static readonly int[] ExtraRates = [128, 192, 256, 320];

    public static int RoundKbps(double kiloBitsPerSecond) =>
        Math.Max(1, (int)Math.Round(kiloBitsPerSecond, MidpointRounding.AwayFromZero));

    public static IReadOnlyList<BitrateChoice> Build(IEnumerable<double> nativeKilobitsPerSecond)
    {
        var native = nativeKilobitsPerSecond
            .Select(RoundKbps)
            .Distinct()
            .OrderByDescending(rate => rate)
            .ToArray();

        var choices = new List<BitrateChoice> { BitrateChoice.Source };
        choices.AddRange(native.Select(rate => new BitrateChoice(rate, true, $"{rate} kbps")));

        foreach (var extra in ExtraRates)
        {
            if (!native.Contains(extra))
                choices.Add(new BitrateChoice(extra, false, $"{extra} kbps"));
        }

        return choices;
    }

    public static AudioBitratePreference ToPreference(BitrateChoice choice)
    {
        if (choice.IsNative || choice.KilobitsPerSecond is null)
            return AudioBitratePreference.Source;

        return choice.KilobitsPerSecond switch
        {
            128 => AudioBitratePreference.Kbps128,
            192 => AudioBitratePreference.Kbps192,
            256 => AudioBitratePreference.Kbps256,
            320 => AudioBitratePreference.Kbps320,
            _ => AudioBitratePreference.Source,
        };
    }

    public static int PickAudioIndex(IReadOnlyList<int> optionKbps, BitrateChoice choice)
    {
        if (optionKbps.Count == 0)
            return -1;

        if (choice.IsNative && choice.KilobitsPerSecond is int kbps)
        {
            for (var i = 0; i < optionKbps.Count; i++)
            {
                if (optionKbps[i] == kbps)
                    return i;
            }
        }

        var best = 0;
        for (var i = 1; i < optionKbps.Count; i++)
        {
            if (optionKbps[i] > optionKbps[best])
                best = i;
        }

        return best;
    }
}
