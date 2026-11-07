using System;
using System.Reflection;
using Avalonia;
using YoutubeDownloader.XC.Utils;

namespace YoutubeDownloader.XC;

public static class Program
{
    private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

    public static string Name { get; } = Assembly.GetName().Name ?? "YoutubeDownloader.XC";

    public static string DisplayName { get; } = "Youtube Downloader XC";

    public static Version Version { get; } = Assembly.GetName().Version ?? new Version(0, 0, 0);

    public static string VersionString { get; } = Version.ToString(3);

    public static bool IsDevelopmentBuild { get; } = Version.Major is <= 0 or >= 999;

    public static string ProjectUrl { get; } = "https://github.com/MIxAxIM/youtube-downloader-xc";

    public static string ProjectReleasesUrl { get; } = $"{ProjectUrl}/releases";

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();

    [STAThread]
    public static int Main(string[] args)
    {
        // Build and run the app
        var builder = BuildAvaloniaApp();

        try
        {
            return builder.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsWindows())
                _ = NativeMethods.Windows.MessageBox(0, ex.ToString(), "Fatal Error", 0x10);

            throw;
        }
        finally
        {
            // Clean up after application shutdown
            if (builder.Instance is IDisposable disposableApp)
                disposableApp.Dispose();
        }
    }
}
