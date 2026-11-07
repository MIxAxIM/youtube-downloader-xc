using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using JsonExtensions.Http;
using JsonExtensions.Reading;
using PowerKit;
using YoutubeDownloader.XC.Core.Utils;

namespace YoutubeDownloader.XC.Core.Tagging;

internal class MusicBrainzClient
{
    // 4 requests per second
    private static readonly ThrottleLock ThrottleLock = new(TimeSpan.FromSeconds(1.0 / 4));

    public async IAsyncEnumerable<MusicBrainzRecording> SearchRecordingsAsync(
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var url =
            "https://musicbrainz.org/ws/2/recording/"
            + "?version=2"
            + "&fmt=json"
            + "&dismax=true"
            + "&limit=100"
            + $"&query={Uri.EscapeDataString(query)}";

        await ThrottleLock.WaitAsync(cancellationToken);
        var json = await Http.Client.GetJsonAsync(url, cancellationToken);

        var recordingsJson =
            json.GetPropertyOrNull("recordings")?.EnumerateArrayOrNull() ?? default;

        foreach (var recordingJson in recordingsJson)
        {
            var artist = recordingJson
                .GetPropertyOrNull("artist-credit")
                ?.EnumerateArrayOrNull()
                ?.FirstOrDefault()
                .GetPropertyOrNull("name")
                ?.GetNonWhiteSpaceStringOrNull();

            if (string.IsNullOrWhiteSpace(artist))
                continue;

            var artistSort = recordingJson
                .GetPropertyOrNull("artist-credit")
                ?.EnumerateArrayOrNull()
                ?.FirstOrDefault()
                .GetPropertyOrNull("artist")
                ?.GetPropertyOrNull("sort-name")
                ?.GetNonWhiteSpaceStringOrNull();

            var title = recordingJson.GetPropertyOrNull("title")?.GetNonWhiteSpaceStringOrNull();

            if (string.IsNullOrWhiteSpace(title))
                continue;

            var firstRelease = recordingJson
                .GetPropertyOrNull("releases")
                ?.EnumerateArrayOrNull()
                ?.FirstOrDefault();

            var album = firstRelease?.GetPropertyOrNull("title")?.GetNonWhiteSpaceStringOrNull();

            int? year = null;
            var dateText = firstRelease?.GetPropertyOrNull("date")?.GetNonWhiteSpaceStringOrNull();
            if (
                !string.IsNullOrWhiteSpace(dateText)
                && dateText.Length >= 4
                && int.TryParse(dateText[..4], out var parsedYear)
            )
            {
                year = parsedYear;
            }

            yield return new MusicBrainzRecording(artist, artistSort, title, album, year);
        }
    }
}
