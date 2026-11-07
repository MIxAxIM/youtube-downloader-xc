using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeDownloader.XC.Core.Utils;

public sealed class SpeedLimiter
{
    public static SpeedLimiter Shared { get; } = new();

    private readonly object _gate = new();
    private long _bytesPerSecond;
    private long _available;
    private long _lastTicks;

    public bool IsUnlimited
    {
        get
        {
            lock (_gate)
                return _bytesPerSecond <= 0;
        }
    }

    public void SetBytesPerSecond(long bytesPerSecond)
    {
        lock (_gate)
        {
            _bytesPerSecond = bytesPerSecond;
            _available = bytesPerSecond > 0 ? bytesPerSecond : 0;
            _lastTicks = 0;
        }
    }

    public TimeSpan DelayFor(int byteCount)
    {
        if (byteCount <= 0)
            return TimeSpan.Zero;

        lock (_gate)
        {
            var limit = _bytesPerSecond;
            if (limit <= 0)
                return TimeSpan.Zero;

            var now = Environment.TickCount64;
            if (_lastTicks == 0)
            {
                _lastTicks = now;
                _available = limit;
            }
            else
            {
                var elapsedMs = Math.Max(0, now - _lastTicks);
                _lastTicks = now;
                var refilled = _available + limit * elapsedMs / 1000;
                _available = Math.Min(limit, refilled);
            }

            if (_available >= byteCount)
            {
                _available -= byteCount;
                return TimeSpan.Zero;
            }

            var deficit = byteCount - _available;
            _available = 0;
            return TimeSpan.FromSeconds(deficit / (double)limit);
        }
    }
}

public sealed class ThrottledStream(Stream inner, SpeedLimiter limiter) : Stream
{
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => throw new NotSupportedException();
    }

    public override void Flush() => inner.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = inner.Read(buffer, offset, count);
        var delay = limiter.DelayFor(read);
        if (delay > TimeSpan.Zero)
            Thread.Sleep(delay);

        return read;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        var read = await inner.ReadAsync(buffer, cancellationToken);
        var delay = limiter.DelayFor(read);
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            inner.Dispose();

        base.Dispose(disposing);
    }
}
