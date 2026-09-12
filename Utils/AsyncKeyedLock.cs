using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HEAppE.Utils;

/// <summary>
/// A reference-counted, key-based asynchronous lock that serializes execution per key
/// while automatically removing unused semaphores and disposing them to prevent memory leaks.
/// </summary>
public sealed class AsyncKeyedLock<TKey> where TKey : notnull
{
    private sealed class RefCountedSemaphore
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount = 1;
    }

    private readonly Dictionary<TKey, RefCountedSemaphore> _locks = new();
    private readonly object _syncRoot = new();

    /// <summary>
    /// Gets the current number of active locked or waiting keys.
    /// Primarily used for verification and diagnostics.
    /// </summary>
    public int ActiveKeyCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _locks.Count;
            }
        }
    }

    /// <summary>
    /// Asynchronously acquires a lock for the specified key.
    /// Returns an <see cref="IDisposable"/> that releases the lock upon disposal.
    /// </summary>
    public async Task<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default)
    {
        RefCountedSemaphore item;
        lock (_syncRoot)
        {
            if (!_locks.TryGetValue(key, out item))
            {
                item = new RefCountedSemaphore();
                _locks.Add(key, item);
            }
            else
            {
                item.RefCount++;
            }
        }

        try
        {
            await item.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_syncRoot)
            {
                item.RefCount--;
                if (item.RefCount == 0)
                {
                    _locks.Remove(key);
                    item.Semaphore.Dispose();
                }
            }
            throw;
        }

        return new Releaser(this, key, item);
    }

    private void Release(TKey key, RefCountedSemaphore item)
    {
        lock (_syncRoot)
        {
            item.RefCount--;
            if (item.RefCount == 0)
            {
                _locks.Remove(key);
                item.Semaphore.Dispose();
                return;
            }
        }

        item.Semaphore.Release();
    }

    private sealed class Releaser : IDisposable
    {
        private AsyncKeyedLock<TKey>? _parent;
        private readonly TKey _key;
        private readonly RefCountedSemaphore _item;

        public Releaser(AsyncKeyedLock<TKey> parent, TKey key, RefCountedSemaphore item)
        {
            _parent = parent;
            _key = key;
            _item = item;
        }

        public void Dispose()
        {
            var parent = Interlocked.Exchange(ref _parent, null);
            parent?.Release(_key, _item);
        }
    }
}
