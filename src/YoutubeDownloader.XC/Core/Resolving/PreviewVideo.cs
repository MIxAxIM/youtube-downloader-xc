using System;
using System.Collections.Generic;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.XC.Core.Resolving;

// Metadata from YouTube's public oEmbed endpoint. Used when the watch page
// refuses anonymous access and asks for a sign-in.
public sealed class PreviewVideo(VideoId id, string title, string author) : IVideo
{
    public VideoId Id { get; } = id;

    public string Url => $"https://www.youtube.com/watch?v={Id}";

    public string Title { get; } = title;

    public Author Author { get; } = new(ChannelId.Parse("UC0000000000000000000000"), author);

    public TimeSpan? Duration => null;

    public IReadOnlyList<Thumbnail> Thumbnails { get; } =
    [new Thumbnail($"https://i.ytimg.com/vi/{id}/hqdefault.jpg", new Resolution(480, 360))];
}
