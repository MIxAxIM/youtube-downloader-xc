using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using YoutubeDownloader.XC.Core.Downloading;
using YoutubeDownloader.XC.Core.Utils;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.XC.Tests;

public class QueueRulesTests
{
    [Fact]
    public void Native_bitrates_stay_supported_and_missing_presets_are_marked()
    {
        var choices = BitrateChoiceList.Build([160.4, 127.6]);

        Assert.Equal(
            ["Source", "160 kbps", "128 kbps", "192 kbps", "256 kbps", "320 kbps"],
            choices.Select(choice => choice.Label).ToArray()
        );
        Assert.True(choices.Single(choice => choice.Label == "Source").IsNative);
        Assert.True(choices.Single(choice => choice.KilobitsPerSecond == 128).IsNative);
        Assert.True(choices.Single(choice => choice.KilobitsPerSecond == 160).IsNative);
        Assert.False(choices.Single(choice => choice.KilobitsPerSecond == 320).IsNative);
        Assert.Equal(
            AudioBitratePreference.Source,
            BitrateChoiceList.ToPreference(
                choices.Single(choice => choice.KilobitsPerSecond == 128)
            )
        );
        Assert.Equal(
            AudioBitratePreference.Kbps320,
            BitrateChoiceList.ToPreference(
                choices.Single(choice => choice.KilobitsPerSecond == 320)
            )
        );
    }

    [Fact]
    public void Audio_pick_uses_the_matching_native_stream()
    {
        var native = new BitrateChoice(160, true, "160 kbps");
        var encoded = new BitrateChoice(320, false, "320 kbps");

        Assert.Equal(1, BitrateChoiceList.PickAudioIndex([128, 160, 70], native));
        Assert.Equal(1, BitrateChoiceList.PickAudioIndex([128, 160, 70], encoded));
    }

    [Fact]
    public void Quality_chips_keep_one_selected_height_for_the_container()
    {
        var chips = QualityChipList.Build(
            [
                new QualityOptionSnapshot("1080p", 1080, "mp4", false, false),
                new QualityOptionSnapshot("720p", 720, "mp4", false, false),
                new QualityOptionSnapshot("720p", 720, "webm", false, false),
                new QualityOptionSnapshot("144p", 144, "mp4", false, true),
                new QualityOptionSnapshot("audio", 0, "mp3", true, false),
            ],
            "mp4",
            null
        );

        Assert.Equal(["1080p", "720p", "144p"], chips.Select(chip => chip.Label).ToArray());
        Assert.Equal(1080, Assert.Single(chips, chip => chip.IsSelected).Height);
    }

    [Fact]
    public void Top_bar_defaults_skip_cards_that_already_started()
    {
        var cards = new[]
        {
            new QueueCardState(false, false, "mp4", null, 1080),
            new QueueCardState(true, false, "mp4", null, 720),
        };

        var updated = QueueDefaults.ApplyAll(cards, true, "m4a", 320);

        Assert.Equal(new QueueCardState(false, true, "m4a", 320, 1080), updated[0]);
        Assert.Equal(cards[1], updated[1]);

        var ownChoice = updated[0] with { SelectedHeight = 480 };
        Assert.Equal(720, updated[1].SelectedHeight);
        Assert.Equal(480, ownChoice.SelectedHeight);
    }

    [Fact]
    public void Progress_bytes_and_combined_speed_follow_the_active_cards()
    {
        Assert.Equal(1_790_000, DownloadProgressMath.DownloadedBytes(0.5, 3_580_000));
        Assert.Equal("1.79 MB", DownloadProgressMath.FormatMegabytes(1_790_000));
        Assert.Equal(0, DownloadProgressMath.DownloadedBytes(0.4, 0));

        var speed = DownloadSpeed.BytesPerSecond(872_930, TimeSpan.FromSeconds(1));
        Assert.Equal(speed, DownloadSpeed.Combine([speed, 0, -1]));
        Assert.Equal("872.93 KB/s", DownloadSpeed.Format(speed));
    }

    [Fact]
    public void History_appends_newest_first_and_drops_past_the_cap()
    {
        IReadOnlyList<DownloadHistoryEntry> history = [];
        for (var i = 0; i < DownloadHistoryList.Cap + 5; i++)
        {
            history = DownloadHistoryList.Append(
                history,
                new DownloadHistoryEntry(
                    $"Video {i}",
                    $@"C:\video{i}.mp4",
                    DateTimeOffset.UnixEpoch
                )
            );
        }

        Assert.Equal(DownloadHistoryList.Cap, history.Count);
        Assert.Equal("Video 204", history[0].Title);
        Assert.Equal($"Video {204 - (DownloadHistoryList.Cap - 1)}", history[^1].Title);
        Assert.Empty(DownloadHistoryList.Clear());
    }

    [Fact]
    public void Audio_lines_use_the_channel_and_leave_missing_fields_empty()
    {
        Assert.Equal("Skye Holland", AudioCardText.Artist("  Skye Holland  "));
        Assert.Equal(AudioCardText.Empty, AudioCardText.Artist(" "));
        Assert.Equal(AudioCardText.Empty, AudioCardText.Genre());
        Assert.Equal(
            "2024-05-01",
            AudioCardText.Date(new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero))
        );
        Assert.Equal(AudioCardText.Empty, AudioCardText.Date(null));
    }

    [Fact]
    public void Audio_formats_include_mp3_and_m4a_and_map_to_ffmpeg()
    {
        var audio = MediaFormats.Audio.Select(format => format.Name).ToArray();
        var video = MediaFormats.Video.Select(format => format.Name).ToArray();

        Assert.Contains("mp3", audio);
        Assert.Contains("m4a", audio);
        Assert.Contains("aac", audio);
        Assert.Contains("opus", audio);
        Assert.Contains("flac", audio);
        Assert.Contains("wav", audio);
        Assert.Contains("mkv", video);
        Assert.Contains("mov", video);
        Assert.Contains("avi", video);
        Assert.Equal("mp4", MediaFormats.ToFfmpegContainer(new Container("m4a"), true).Name);
        Assert.Equal("adts", MediaFormats.ToFfmpegContainer(new Container("aac"), true).Name);
        Assert.Equal("matroska", MediaFormats.ToFfmpegContainer(new Container("mkv"), false).Name);
    }

    [Fact]
    public void Speed_limiter_allows_one_burst_then_delays()
    {
        var limiter = new SpeedLimiter();
        limiter.SetBytesPerSecond(1_000);

        Assert.Equal(TimeSpan.Zero, limiter.DelayFor(1_000));
        var delay = limiter.DelayFor(500);
        Assert.InRange(delay.TotalMilliseconds, 400, 500);

        limiter.SetBytesPerSecond(0);
        Assert.Equal(TimeSpan.Zero, limiter.DelayFor(50_000_000));
    }
}
