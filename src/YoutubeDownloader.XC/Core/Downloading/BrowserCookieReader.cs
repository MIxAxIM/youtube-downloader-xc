using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace YoutubeDownloader.XC.Core.Downloading;

public static class BrowserCookieReader
{
    public static IReadOnlyList<Cookie> TryReadYouTubeCookies()
    {
        try
        {
            foreach (var profile in CandidateProfiles())
            {
                var cookiesPath = Path.Combine(profile, "Network", "Cookies");
                if (!File.Exists(cookiesPath))
                    cookiesPath = Path.Combine(profile, "Cookies");
                if (!File.Exists(cookiesPath))
                    continue;

                // Locked Chromium DBs fail quietly.
                using var stream = new FileStream(
                    cookiesPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );
                using var reader = new StreamReader(
                    stream,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 4096,
                    leaveOpen: false
                );
                var text = reader.ReadToEnd();
                if (!text.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Binary SQLite; without a full parser we only confirm presence and return empty.
                // Sign-in window remains the reliable path when the profile is locked or encrypted.
                return [];
            }
        }
        catch
        {
            // Ignore locked or unreadable profiles.
        }

        return [];
    }

    private static IEnumerable<string> CandidateProfiles()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(local, "Google", "Chrome", "User Data", "Default");
        yield return Path.Combine(local, "Microsoft", "Edge", "User Data", "Default");
    }
}
