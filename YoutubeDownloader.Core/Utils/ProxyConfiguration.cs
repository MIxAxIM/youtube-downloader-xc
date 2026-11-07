using System;

namespace YoutubeDownloader.Core.Utils;

public enum ProxyProtocol
{
    Http,
    Socks4,
    Socks5,
}

public sealed record ProxyConfiguration(
    bool IsEnabled,
    ProxyProtocol Protocol,
    string? Host,
    int Port,
    string? Username,
    string? Password
)
{
    public static ProxyConfiguration Disabled { get; } =
        new(false, ProxyProtocol.Http, null, 0, null, null);

    public Uri? TryCreateProxyUri()
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(Host) || Port is < 1 or > 65535)
            return null;

        // SOCKS4 cannot carry hostnames. SOCKS4a lets YouTube names resolve at the proxy.
        var scheme = Protocol switch
        {
            ProxyProtocol.Http => "http",
            ProxyProtocol.Socks4 => "socks4a",
            ProxyProtocol.Socks5 => "socks5",
            _ => throw new ArgumentOutOfRangeException(nameof(Protocol), Protocol, null),
        };

        try
        {
            return new UriBuilder(scheme, Host.Trim(), Port).Uri;
        }
        catch (UriFormatException)
        {
            return null;
        }
    }
}
