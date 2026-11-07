using System;

namespace YoutubeDownloader.XC.Core.Downloading;

public enum AudioBitratePreference
{
    Source,
    Kbps128,
    Kbps192,
    Kbps256,
    Kbps320,
}

public static class AudioBitratePreferenceExtensions
{
    extension(AudioBitratePreference preference)
    {
        public int? TargetKbps =>
            preference switch
            {
                AudioBitratePreference.Source => null,
                AudioBitratePreference.Kbps128 => 128,
                AudioBitratePreference.Kbps192 => 192,
                AudioBitratePreference.Kbps256 => 256,
                AudioBitratePreference.Kbps320 => 320,
                _ => throw new ArgumentOutOfRangeException(nameof(preference), preference, null),
            };

        public string GetDisplayName() =>
            preference switch
            {
                AudioBitratePreference.Source => "Source",
                AudioBitratePreference.Kbps128 => "128 kbps",
                AudioBitratePreference.Kbps192 => "192 kbps",
                AudioBitratePreference.Kbps256 => "256 kbps",
                AudioBitratePreference.Kbps320 => "320 kbps",
                _ => throw new ArgumentOutOfRangeException(nameof(preference), preference, null),
            };
    }
}
