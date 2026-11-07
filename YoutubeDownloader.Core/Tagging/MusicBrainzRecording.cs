namespace YoutubeDownloader.Core.Tagging;

public record MusicBrainzRecording(
    string Artist,
    string? ArtistSort,
    string Title,
    string? Album,
    int? Year = null
);
