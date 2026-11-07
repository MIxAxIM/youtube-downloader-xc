using System.Threading;
using System.Threading.Tasks;

namespace YoutubeDownloader.XC.Core.Downloading;

public sealed class DownloadPauseGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _paused;

    public bool IsPaused => Volatile.Read(ref _paused) == 1;

    public void Pause()
    {
        if (Interlocked.Exchange(ref _paused, 1) == 0)
            _gate.Wait(0);
    }

    public void Resume()
    {
        if (Interlocked.Exchange(ref _paused, 0) == 1 && _gate.CurrentCount == 0)
            _gate.Release();
    }

    public async Task WaitIfPausedAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPaused)
            return;

        await _gate.WaitAsync(cancellationToken);
        _gate.Release();
    }
}
