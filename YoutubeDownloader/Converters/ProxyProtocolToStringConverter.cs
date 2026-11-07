using System;
using System.Globalization;
using Avalonia.Data.Converters;
using YoutubeDownloader.Core.Utils;

namespace YoutubeDownloader.Converters;

public class ProxyProtocolToStringConverter : IValueConverter
{
    public static ProxyProtocolToStringConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProxyProtocol protocol)
        {
            return protocol switch
            {
                ProxyProtocol.Http => "HTTP",
                ProxyProtocol.Socks4 => "SOCKS4",
                ProxyProtocol.Socks5 => "SOCKS5",
                _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, null),
            };
        }

        return default(string);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
