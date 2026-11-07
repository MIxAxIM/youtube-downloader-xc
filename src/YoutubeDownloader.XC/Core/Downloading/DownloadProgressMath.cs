using System;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeDownloader.XC.Core.Downloading;

public static class DownloadProgressMath
{
    public static long DownloadedBytes(double fraction, long totalBytes)
    {
        if (totalBytes <= 0)
            return 0;

        var clamped = Math.Clamp(fraction, 0, 1);
        return (long)(clamped * totalBytes);
    }

    public static string FormatMegabytes(long bytes) => $"{bytes / 1_000_000d:0.00} MB";
}

public static class DownloadSpeed
{
    public static double BytesPerSecond(long byteDelta, TimeSpan elapsed)
    {
        if (byteDelta <= 0 || elapsed <= TimeSpan.Zero)
            return 0;

        return byteDelta / elapsed.TotalSeconds;
    }

    public static double Combine(IEnumerable<double> bytesPerSecond) =>
        bytesPerSecond.Where(speed => speed > 0).Sum();

    public static string Format(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1_000_000d)
            return $"{bytesPerSecond / 1_000_000d:0.00} MB/s";

        return $"{bytesPerSecond / 1_000d:0.00} KB/s";
    }
}
