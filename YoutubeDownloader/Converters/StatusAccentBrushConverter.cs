using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace YoutubeDownloader.Converters;

public sealed class StatusAccentBrushConverter : IValueConverter
{
    public static StatusAccentBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value as string ?? "AppStatusReadyBrush";
        if (
            Application.Current?.TryGetResource(
                key,
                Application.Current.ActualThemeVariant,
                out var resource
            ) == true
            && resource is IBrush brush
        )
        {
            return brush;
        }

        return Brushes.Gray;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
