using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeDownloader.XC.Core.Downloading;

public static class FfmpegPostProcess
{
    public static async Task ExtractCoverFrameAsync(
        string videoPath,
        string imagePath,
        TimeSpan time,
        string ffmpegFilePath,
        CancellationToken cancellationToken = default
    )
    {
        var arguments = new List<string>
        {
            "-hide_banner",
            "-loglevel",
            "error",
            "-ss",
            time.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture),
            "-i",
            videoPath,
            "-frames:v",
            "1",
            "-y",
            imagePath,
        };
        await RunAsync(ffmpegFilePath, arguments, cancellationToken);
    }

    public static async Task TrimClipAsync(
        string sourcePath,
        string outputPath,
        TimeSpan? start,
        TimeSpan? end,
        string ffmpegFilePath,
        CancellationToken cancellationToken = default
    )
    {
        var arguments = new List<string> { "-hide_banner", "-loglevel", "error", "-nostdin" };

        if (start is not null)
        {
            arguments.Add("-ss");
            arguments.Add(start.Value.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        }

        arguments.Add("-i");
        arguments.Add(sourcePath);

        if (end is not null)
        {
            var duration = end.Value - (start ?? TimeSpan.Zero);
            if (duration > TimeSpan.Zero)
            {
                arguments.Add("-t");
                arguments.Add(
                    duration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)
                );
            }
        }

        arguments.Add("-c");
        arguments.Add("copy");
        arguments.Add("-y");
        arguments.Add(outputPath);

        await RunAsync(ffmpegFilePath, arguments, cancellationToken);
    }

    public static async Task WriteSrtAsync(
        string srtPath,
        IEnumerable<(TimeSpan Start, TimeSpan End, string Text)> cues,
        CancellationToken cancellationToken = default
    )
    {
        await using var writer = new StreamWriter(srtPath);
        var index = 1;
        foreach (var cue in cues)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(index.ToString());
            await writer.WriteLineAsync($"{FormatSrt(cue.Start)} --> {FormatSrt(cue.End)}");
            await writer.WriteLineAsync(cue.Text);
            await writer.WriteLineAsync();
            index++;
        }
    }

    private static string FormatSrt(TimeSpan time) =>
        $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";

    private static async Task RunAsync(
        string ffmpegFilePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken
    )
    {
        using var process = new Process();
        process.StartInfo.FileName = ffmpegFilePath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        if (!process.Start())
            throw new InvalidOperationException("Failed to start FFmpeg.");

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? $"FFmpeg exited with code {process.ExitCode}."
                    : error.Trim()
            );
        }
    }
}
