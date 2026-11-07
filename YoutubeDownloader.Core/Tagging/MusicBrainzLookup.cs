using System;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.Core.Tagging;

public class MusicBrainzLookup
{
    private readonly MusicBrainzClient _client = new();

    public async Task<MusicBrainzRecording?> FindBestAsync(
        IVideo video,
        CancellationToken cancellationToken = default
    )
    {
        await foreach (
            var recording in _client.SearchRecordingsAsync(video.Title, cancellationToken)
        )
        {
            if (
                video.Title.Contains(recording.Title, StringComparison.OrdinalIgnoreCase)
                && (
                    video.Title.Contains(recording.Artist, StringComparison.OrdinalIgnoreCase)
                    || video.Author.ChannelTitle.Contains(
                        recording.Artist,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                return recording;
            }
        }

        return null;
    }
}
