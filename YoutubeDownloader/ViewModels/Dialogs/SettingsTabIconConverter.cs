using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace YoutubeDownloader.ViewModels.Dialogs;

public sealed class SettingsTabIconConverter : IValueConverter
{
    public static SettingsTabIconConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is SettingsTab tab
            ? tab switch
            {
                SettingsTab.General => MaterialIconKind.CogOutline,
                SettingsTab.Download => MaterialIconKind.Download,
                SettingsTab.Network => MaterialIconKind.Lan,
                SettingsTab.Audio => MaterialIconKind.MusicNote,
                SettingsTab.Video => MaterialIconKind.VideoOutline,
                SettingsTab.Tags => MaterialIconKind.TagOutline,
                SettingsTab.Authorization => MaterialIconKind.AccountKeyOutline,
                _ => MaterialIconKind.CogOutline,
            }
            : MaterialIconKind.CogOutline;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
