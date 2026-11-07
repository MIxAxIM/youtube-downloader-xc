using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDownloader.XC.Core.Utils;
using YoutubeDownloader.XC.Core.Utils.Extensions;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.XC.Core.Tagging;

public class MediaTagInjector
{
    private readonly MusicBrainzClient _musicBrainz = new();

    private void InjectMiscMetadata(MediaFile mediaFile, IVideo video, string? commentOverride)
    {
        var description = (video as Video)?.Description;
        if (!string.IsNullOrWhiteSpace(description))
            mediaFile.SetDescription(description);

        mediaFile.SetComment(
            !string.IsNullOrWhiteSpace(commentOverride)
                ? commentOverride
                : $"""
                Downloaded using YoutubeDownloader.XC (https://github.com/Tyrrrz/YoutubeDownloader.XC)
                Video: {video.Title}
                Video URL: {video.Url}
                Channel: {video.Author.ChannelTitle}
                Channel URL: {video.Author.ChannelUrl}
                """
        );
    }

    private void InjectChapters(MediaFile mediaFile, IVideo video, IReadOnlyList<string>? chapters)
    {
        var lines = chapters ?? ChapterList.FromDescription((video as Video)?.Description);
        if (lines.Count == 0)
            return;

        mediaFile.SetComment(
            string.Join(Environment.NewLine, ["Chapters:", .. lines, "", $"Video URL: {video.Url}"])
        );
    }

    private async Task InjectThumbnailAsync(
        MediaFile mediaFile,
        IVideo video,
        CancellationToken cancellationToken = default
    )
    {
        var thumbnailUrl =
            video
                .Thumbnails.Where(t =>
                    string.Equals(t.TryGetImageFormat(), "jpg", StringComparison.OrdinalIgnoreCase)
                )
                .OrderByDescending(t => t.Resolution.Area)
                .Select(t => t.Url)
                .FirstOrDefault()
            ?? $"https://i.ytimg.com/vi/{video.Id}/hqdefault.jpg";

        mediaFile.SetThumbnail(
            await Http.Client.GetByteArrayAsync(thumbnailUrl, cancellationToken)
        );
    }

    public async Task InjectTagsAsync(
        string filePath,
        IVideo video,
        bool audioOnly = false,
        CancellationToken cancellationToken = default,
        string? title = null,
        string? artist = null,
        string? album = null,
        int? year = null,
        bool writeArtwork = true,
        string? commentOverride = null,
        bool writeChapters = true,
        IReadOnlyList<string>? chapters = null
    )
    {
        using var mediaFile = MediaFile.Open(filePath);

        var resolvedTitle = string.IsNullOrWhiteSpace(title) ? video.Title : title.Trim();
        var resolvedArtist = string.IsNullOrWhiteSpace(artist)
            ? video.Author.ChannelTitle
            : artist.Trim();

        mediaFile.SetTitle(resolvedTitle);
        if (!string.IsNullOrWhiteSpace(resolvedArtist))
            mediaFile.SetArtist(resolvedArtist);
        if (!string.IsNullOrWhiteSpace(album))
            mediaFile.SetAlbum(album.Trim());
        if (year is > 0)
            mediaFile.SetYear((uint)year.Value);

        if (writeArtwork)
        {
            try
            {
                await InjectThumbnailAsync(mediaFile, video, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Cover art is best-effort
            }
        }

        // Only fill empty fields from MusicBrainz; never overwrite user values.
        if (
            string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(artist)
            || string.IsNullOrWhiteSpace(album)
            || year is null
        )
        {
            try
            {
                MusicBrainzRecording? recording = null;
                await foreach (
                    var candidate in _musicBrainz.SearchRecordingsAsync(
                        video.Title,
                        cancellationToken
                    )
                )
                {
                    if (
                        video.Title.Contains(candidate.Title, StringComparison.OrdinalIgnoreCase)
                        && (
                            video.Title.Contains(
                                candidate.Artist,
                                StringComparison.OrdinalIgnoreCase
                            )
                            || video.Author.ChannelTitle.Contains(
                                candidate.Artist,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    {
                        recording = candidate;
                        break;
                    }
                }

                if (recording is not null)
                {
                    if (string.IsNullOrWhiteSpace(title))
                        mediaFile.SetTitle(recording.Title);
                    if (string.IsNullOrWhiteSpace(artist))
                        mediaFile.SetArtist(recording.Artist);
                    if (
                        string.IsNullOrWhiteSpace(album)
                        && !string.IsNullOrWhiteSpace(recording.Album)
                    )
                        mediaFile.SetAlbum(recording.Album);
                    if (year is null && recording.Year is > 0)
                        mediaFile.SetYear((uint)recording.Year.Value);
                    if (!string.IsNullOrWhiteSpace(recording.ArtistSort))
                        mediaFile.SetArtistSort(recording.ArtistSort);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // MusicBrainz is optional
            }
        }

        if (writeChapters)
            InjectChapters(mediaFile, video, chapters);

        InjectMiscMetadata(mediaFile, video, commentOverride);
        mediaFile.Save();
        _ = audioOnly;
    }
}
