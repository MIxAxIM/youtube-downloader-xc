using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gress;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.Core.Downloading;

internal static class AudioEncoder
{
    public static async Task DownloadAsync(
        YoutubeClient youtube,
        string filePath,
        IAudioStreamInfo streamInfo,
        Container container,
        AudioBitratePreference bitrate,
        string ffmpegFilePath,
        IProgress<Percentage>? progress,
        CancellationToken cancellationToken,
        bool applyLoudnorm = false,
        DownloadPauseGate? pauseGate = null
    )
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            if (pauseGate is not null)
                await pauseGate.WaitIfPausedAsync(cancellationToken);

            await youtube.Videos.Streams.DownloadAsync(
                streamInfo,
                tempFilePath,
                progress is null
                    ? null
                    : new Progress<double>(value =>
                        progress.Report(Percentage.FromFraction(Math.Clamp(value, 0, 1) * 0.9))
                    ),
                cancellationToken
            );

            if (pauseGate is not null)
                await pauseGate.WaitIfPausedAsync(cancellationToken);

            var copySource =
                bitrate == AudioBitratePreference.Source
                && string.Equals(
                    streamInfo.Container.Name,
                    "mp4",
                    StringComparison.OrdinalIgnoreCase
                )
                && IsM4a(container)
                && !applyLoudnorm;

            var (format, codec, ignoresBitrate) = GetEncodeTarget(container);
            var arguments = new List<string>
            {
                "-hide_banner",
                "-loglevel",
                "error",
                "-nostdin",
                "-i",
                tempFilePath,
                "-vn",
            };

            if (applyLoudnorm)
            {
                arguments.Add("-af");
                arguments.Add("loudnorm");
            }

            if (copySource)
            {
                arguments.Add("-c");
                arguments.Add("copy");
            }
            else
            {
                arguments.Add("-c:a");
                arguments.Add(codec);
                if (!ignoresBitrate)
                {
                    var kbps =
                        bitrate.TargetKbps
                        ?? Math.Max(32, (int)Math.Round(streamInfo.Bitrate.KiloBitsPerSecond));
                    arguments.Add("-b:a");
                    arguments.Add($"{kbps}k");
                }
            }

            arguments.Add("-f");
            arguments.Add(format);
            arguments.Add("-y");
            arguments.Add(filePath);

            await RunFfmpegAsync(ffmpegFilePath, arguments, cancellationToken);
            progress?.Report(Percentage.FromFraction(1));
        }
        finally
        {
            try
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
            catch
            {
                // Ignore
            }
        }
    }

    public static async Task ExtractAudioAsync(
        string sourceFilePath,
        string outputFilePath,
        Container container,
        string ffmpegFilePath,
        CancellationToken cancellationToken = default,
        bool applyLoudnorm = false
    )
    {
        var (format, codec, _) = GetEncodeTarget(container);
        var arguments = new List<string>
        {
            "-hide_banner",
            "-loglevel",
            "error",
            "-nostdin",
            "-i",
            sourceFilePath,
            "-vn",
            "-c:a",
            codec,
        };

        if (applyLoudnorm)
        {
            arguments.Add("-af");
            arguments.Add("loudnorm");
        }

        arguments.Add("-f");
        arguments.Add(format);
        arguments.Add("-y");
        arguments.Add(outputFilePath);

        await RunFfmpegAsync(ffmpegFilePath, arguments, cancellationToken);
    }

    public static bool NeedsCustomMux(Container container, AudioBitratePreference bitrate) =>
        bitrate != AudioBitratePreference.Source
        || MediaFormats.RequiresCustomAudioMux(container)
        || IsM4a(container);

    private static bool IsM4a(Container container) =>
        string.Equals(container.Name, "m4a", StringComparison.OrdinalIgnoreCase);

    private static (string Format, string Codec, bool IgnoresBitrate) GetEncodeTarget(
        Container container
    )
    {
        var format =
            MediaFormats.Find(container.Name, audioOnly: true)
            ?? throw new InvalidOperationException(
                $"Can't encode audio into container '{container.Name}'."
            );

        if (string.IsNullOrWhiteSpace(format.AudioCodec))
        {
            throw new InvalidOperationException(
                $"Can't encode audio into container '{container.Name}'."
            );
        }

        return (format.FfmpegMuxer, format.AudioCodec, format.IgnoresBitrate);
    }

    private static async Task RunFfmpegAsync(
        string ffmpegFilePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken
    )
    {
        using var process = new Process();
        process.StartInfo.FileName = ffmpegFilePath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        if (!process.Start())
            throw new InvalidOperationException("Failed to start FFmpeg.");

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore
            }

            throw;
        }

        _ = await standardOutput;
        var error = await standardError;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? $"FFmpeg exited with code {process.ExitCode}."
                    : error.Trim()
            );
        }
    }
}
