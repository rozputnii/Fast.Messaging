using DotNext.Threading;

namespace Fast.Messaging.Internal;

internal sealed class AsyncLock : IAsyncDisposable
{
    private readonly AsyncExclusiveLock _lock = new();

    public async ValueTask DisposeAsync()
    {
        await _lock.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask<AsyncLockRelease> LockAsync(CancellationToken ct = default)
    {
        await _lock.AcquireAsync(ct).ConfigureAwait(false);
        return new AsyncLockRelease(_lock);
    }

    public readonly struct AsyncLockRelease : IDisposable
    {
        private readonly AsyncExclusiveLock _lock;

        internal AsyncLockRelease(AsyncExclusiveLock @lock)
        {
            _lock = @lock;
        }
        public void Dispose()
        {
            _lock?.Release();
        }
    }
}