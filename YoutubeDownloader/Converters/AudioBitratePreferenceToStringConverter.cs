using System;
using System.Globalization;
using Avalonia.Data.Converters;
using YoutubeDownloader.Core.Downloading;

namespace YoutubeDownloader.Converters;

public class AudioBitratePreferenceToStringConverter : IValueConverter
{
    public static AudioBitratePreferenceToStringConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AudioBitratePreference preference)
            return preference.GetDisplayName();

        return default(string);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
