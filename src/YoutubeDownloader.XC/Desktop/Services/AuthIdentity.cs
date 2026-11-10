using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeDownloader.XC.Core.Utils;

namespace YoutubeDownloader.XC.Services;

public static class AuthIdentity
{
    private static readonly Regex EmailRegex = new(
        @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled
    );

    private static readonly Regex AccountBylineRegex = new(
        "\"accountByline\"\\s*:\\s*\\{\\s*\"simpleText\"\\s*:\\s*\"(?<value>[^\"]+)\"",
        RegexOptions.Compiled
    );

    private static readonly Regex AccountNameRegex = new(
        "\"accountName\"\\s*:\\s*\\{\\s*\"simpleText\"\\s*:\\s*\"(?<value>[^\"]+)\"",
        RegexOptions.Compiled
    );

    private static readonly Regex ChannelHandleRegex = new(
        "\"channelHandle\"\\s*:\\s*\\{\\s*\"simpleText\"\\s*:\\s*\"(?<value>@[^\"]+)\"",
        RegexOptions.Compiled
    );

    public static string? Label { get; private set; }

    public static event Action? LabelChanged;

    public static bool IsSignedIn(IReadOnlyList<Cookie>? cookies) => cookies is { Count: > 0 };

    public static async Task RefreshAsync(IReadOnlyList<Cookie>? cookies)
    {
        if (!IsSignedIn(cookies))
        {
            SetLabel(null);
            return;
        }

        var label = FindInCookies(cookies!) ?? await FindOnYouTubeAsync(cookies!);
        SetLabel(string.IsNullOrWhiteSpace(label) ? null : label);
    }

    private static void SetLabel(string? label)
    {
        if (Label == label)
            return;

        Label = label;
        LabelChanged?.Invoke();
    }

    private static string? FindInCookies(IReadOnlyList<Cookie> cookies)
    {
        foreach (var cookie in cookies)
        {
            var email = FirstEmail(cookie.Value);
            if (email is not null)
                return email;
        }

        return null;
    }

    private static async Task<string?> FindOnYouTubeAsync(IReadOnlyList<Cookie> cookies)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.youtube.com/");
            request.Headers.TryAddWithoutValidation("Cookie", CookieHeader(cookies));
            request.Headers.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
            );
            request.Headers.TryAddWithoutValidation("Accept-Language", "en");

            using var response = await Http.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var html = await response.Content.ReadAsStringAsync();
            return FirstMatch(AccountBylineRegex, html)
                ?? FirstEmail(html)
                ?? FirstMatch(ChannelHandleRegex, html)
                ?? FirstMatch(AccountNameRegex, html);
        }
        catch
        {
            return null;
        }
    }

    private static string CookieHeader(IReadOnlyList<Cookie> cookies) =>
        string.Join(
            "; ",
            cookies
                .Where(cookie =>
                    cookie.Domain.Contains("youtube", StringComparison.OrdinalIgnoreCase)
                    || cookie.Domain.Contains("google", StringComparison.OrdinalIgnoreCase)
                )
                .Select(cookie => $"{cookie.Name}={cookie.Value}")
        );

    private static string? FirstMatch(Regex pattern, string text)
    {
        var match = pattern.Match(text);
        if (!match.Success)
            return null;

        var value = match.Groups["value"].Value.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? FirstEmail(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        foreach (Match match in EmailRegex.Matches(text))
        {
            var email = match.Value;
            if (
                email.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                || email.Contains("google.com", StringComparison.OrdinalIgnoreCase)
                || email.Contains("gstatic.com", StringComparison.OrdinalIgnoreCase)
                || email.Contains("schema.org", StringComparison.OrdinalIgnoreCase)
            )
                continue;

            return email;
        }

        return null;
    }
}
