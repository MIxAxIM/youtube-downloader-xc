using System;
using System.Collections.Generic;
using System.Linq;
using YoutubeDownloader.Core.Downloading;
using Xunit;

namespace YoutubeDownloader.Tests;

public class QueuePolicyTests
{
    [Fact]
    public void Completed_card_has_no_progress_fill()
    {
        Assert.Equal(0, QueuePolicy.ProgressFill(completed: true, started: false, fraction: 1));
        Assert.Equal(0.4, QueuePolicy.ProgressFill(completed: false, started: true, fraction: 0.4));
    }

    [Fact]
    public void Shorts_and_watch_links_are_the_same_video()
    {
        var watch = QueuePolicy.CanonicalVideoId("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        var shorts = QueuePolicy.CanonicalVideoId("https://www.youtube.com/shorts/dQw4w9WgXcQ");
        var music = QueuePolicy.CanonicalVideoId("https://music.youtube.com/watch?v=dQw4w9WgXcQ");

        Assert.Equal(watch, shorts);
        Assert.Equal(watch, music);
        Assert.True(QueuePolicy.IsDuplicate([watch], shorts));
        Assert.False(QueuePolicy.IsDuplicate([watch], "https://youtu.be/abcdefghijk"));
    }

    [Fact]
    public void Time_remaining_uses_bytes_left_and_speed()
    {
        var remaining = QueuePolicy.TimeRemaining(1_000, 500, 100);
        Assert.Equal(TimeSpan.FromSeconds(5), remaining);
        Assert.Null(QueuePolicy.TimeRemaining(1_000, 500, 0));
    }

    [Fact]
    public void Edited_title_becomes_the_file_name()
    {
        Assert.Equal("My Song.m4a", QueuePolicy.FileNameFromTitle("My Song", "m4a"));
    }

    [Fact]
    public void Video_and_audio_use_different_folders()
    {
        Assert.Equal(@"E:\Music", QueuePolicy.OutputFolder(true, @"E:\Video", @"E:\Music", false, null, false));
        Assert.Equal(@"E:\Video", QueuePolicy.OutputFolder(false, @"E:\Video", @"E:\Music", false, null, false));
        Assert.Equal(@"E:\Music", QueuePolicy.OutputFolder(false, @"E:\Video", @"E:\Music", true, null, false));
        Assert.Equal(
            PathCombine(@"E:\Music", "Mix"),
            QueuePolicy.OutputFolder(true, @"E:\Video", @"E:\Music", false, "Mix", true)
        );
    }

    [Fact]
    public void Filter_hides_a_card_without_removing_it()
    {
        Assert.False(QueuePolicy.IsCardVisible("Alpha", "beta", "All", false, "Ready"));
        Assert.True(QueuePolicy.IsCardVisible("Alpha", "alp", "Audio", true, "Ready"));
        Assert.False(QueuePolicy.IsCardVisible("Alpha", "", "Completed", true, "Ready"));
    }

    [Fact]
    public void Simultaneous_limit_stays_between_one_and_twenty()
    {
        Assert.Equal(1, QueuePolicy.ClampSimultaneous(0));
        Assert.Equal(20, QueuePolicy.ClampSimultaneous(50));
        Assert.Equal(8, QueuePolicy.ClampSimultaneous(8));
    }

    [Fact]
    public void Proxy_is_applied_only_when_complete_and_test_requires_204()
    {
        Assert.False(QueuePolicy.ShouldApplyProxy(true, "", 8080));
        Assert.True(QueuePolicy.ShouldApplyProxy(true, "127.0.0.1", 8080));
        Assert.True(QueuePolicy.IsProxyTestSuccess(204));
        Assert.False(QueuePolicy.IsProxyTestSuccess(200));
    }

    [Fact]
    public void Emoji_is_removed_and_existing_files_are_skipped_only_when_asked()
    {
        Assert.Equal("Song", QueuePolicy.RemoveEmoji("Song 🎵"));
        Assert.True(QueuePolicy.ShouldSkipExisting(true, true));
        Assert.False(QueuePolicy.ShouldSkipExisting(false, true));
    }

    [Fact]
    public void Typed_artist_wins_and_year_follows_the_selected_source()
    {
        Assert.Equal(
            "Typed",
            QueuePolicy.ResolveArtist("Typed", "Brainz", "Channel", true)
        );
        Assert.Equal(
            "Channel",
            QueuePolicy.ResolveArtist(" ", null, "Channel", true)
        );
        Assert.Equal(2020, QueuePolicy.ResolveYear("Upload", 2020, 1999));
        Assert.Equal(1999, QueuePolicy.ResolveYear("MusicBrainz", 2020, 1999));
        Assert.Null(QueuePolicy.ResolveYear("None", 2020, 1999));
        Assert.True(QueuePolicy.ShouldWriteDescription(true));
        Assert.False(QueuePolicy.ShouldWriteDescription(false));
    }

    [Fact]
    public void History_filter_drops_old_rows_and_export_keeps_visible_ones()
    {
        var now = DateTimeOffset.Parse("2026-11-02T00:00:00Z");
        var rows = new[]
        {
            new Row("new", now.AddHours(-1)),
            new Row("old", now.AddDays(-40)),
        };

        var visible = QueuePolicy.FilterHistoryByAge(rows, row => row.At, "30Days", now);
        Assert.Equal(["new"], visible.Select(row => row.Name).ToArray());
        Assert.Equal(2, QueuePolicy.FilterHistoryByAge(rows, row => row.At, "All", now).Count);
    }

    [Fact]
    public void Clip_size_video_only_and_srt_follow_the_options()
    {
        var clip = QueuePolicy.ParseClip("1:30", "2:00");
        Assert.Equal(TimeSpan.FromSeconds(90), clip.Start);
        Assert.Equal(TimeSpan.FromMinutes(2), clip.End);
        Assert.Equal(300, QueuePolicy.EstimateSize([100, 200]));
        Assert.False(QueuePolicy.KeepsAudio(videoOnly: true));
        Assert.True(QueuePolicy.ShouldWriteSrt(true));
        Assert.False(QueuePolicy.ShouldWriteSrt(false));
    }

    [Fact]
    public void Members_only_metered_and_portable_settings_are_recognized()
    {
        Assert.True(QueuePolicy.IsMembersOrPremiere("This video is members-only"));
        Assert.True(QueuePolicy.IsMembersOrPremiere("Premiere has not started"));
        Assert.False(QueuePolicy.ShouldStartDownload(networkIsMetered: true, pauseOnMetered: true));
        Assert.True(QueuePolicy.ShouldStartDownload(networkIsMetered: true, pauseOnMetered: false));
        Assert.EndsWith("Settings.json", QueuePolicy.SettingsFilePath(true, @"E:\App", @"C:\profile\settings.json"));
        Assert.Equal(@"C:\profile\settings.json", QueuePolicy.SettingsFilePath(false, @"E:\App", @"C:\profile\settings.json"));
    }

    [Fact]
    public void Insert_retry_disk_and_reorder_follow_the_queue_rules()
    {
        Assert.Equal(0, QueuePolicy.InsertIndex(addOnTop: true, count: 4));
        Assert.Equal(4, QueuePolicy.InsertIndex(addOnTop: false, count: 4));
        Assert.True(QueuePolicy.CanAutoRetry(1, canceled: false));
        Assert.False(QueuePolicy.CanAutoRetry(2, canceled: false));
        Assert.False(QueuePolicy.CanAutoRetry(0, canceled: true));
        Assert.False(QueuePolicy.HasEnoughDiskSpace(900, 1_000, 200));
        Assert.Equal([2, 0, 1], QueuePolicy.Move([0, 1, 2], 2, 0));
        Assert.False(QueuePolicy.IsValidCompletedFile(null));
        Assert.False(QueuePolicy.IsValidCompletedFile(@"C:\missing\file.mp4"));
    }

    [Fact]
    public void History_export_writes_visible_rows()
    {
        var text = DownloadHistoryList.ExportVisible(
            [
                new DownloadHistoryEntry("A", @"E:\a.mp4", DateTimeOffset.Parse("2026-01-01Z"), "https://youtu.be/a"),
                new DownloadHistoryEntry("B", @"E:\b.mp4", DateTimeOffset.Parse("2026-01-02Z"), "https://youtu.be/b"),
            ]
        );
        Assert.Contains("A", text);
        Assert.Contains("https://youtu.be/b", text);
    }

    [Fact]
    public void Settings_export_omits_secrets_and_queue_status_resets_downloads()
    {
        var json = SettingsExport.ToJson(
            new Dictionary<string, object?>
            {
                ["ParallelLimit"] = 4,
                ["ProxyPassword"] = "secret",
                ["LastAuthCookies"] = "cookie",
            }
        );
        Assert.Contains("ParallelLimit", json);
        Assert.DoesNotContain("secret", json);
        Assert.DoesNotContain("cookie", json);
        Assert.Equal("Ready", QueueSnapshot.NormalizeStatus("Started"));
        Assert.Equal("Completed", QueueSnapshot.NormalizeStatus("Completed"));
    }

    [Fact]
    public void General_automation_insert_and_download_folder_rules()
    {
        Assert.Equal(0, QueuePolicy.InsertIndex(addOnTop: true, 5));
        Assert.Equal(5, QueuePolicy.InsertIndex(addOnTop: false, 5));
        Assert.Equal(
            @"E:\Music\Mix",
            QueuePolicy.OutputFolder(true, @"E:\Video", @"E:\Music", false, "Mix", true)
        );
        Assert.Equal(
            @"E:\Music",
            QueuePolicy.OutputFolder(false, @"E:\Video", @"E:\Music", true, "Mix", false)
        );
    }

    private static string PathCombine(string root, string child) =>
        System.IO.Path.Combine(root, child);

    private sealed record Row(string Name, DateTimeOffset At);
}
