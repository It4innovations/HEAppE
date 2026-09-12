using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.Utils;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Common;

public class AsyncKeyedLockTests
{
    [Fact]
    public async Task LockAsync_SerializesConcurrentRequestsForSameKey()
    {
        var keyedLock = new AsyncKeyedLock<long>();
        long key = 42;
        int activeInCriticalSection = 0;
        int maxConcurrent = 0;
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using (await keyedLock.LockAsync(key))
                {
                    int count = Interlocked.Increment(ref activeInCriticalSection);
                    lock (tasks)
                    {
                        if (count > maxConcurrent) maxConcurrent = count;
                    }
                    await Task.Delay(20);
                    Interlocked.Decrement(ref activeInCriticalSection);
                }
            }));
        }

        await Task.WhenAll(tasks);

        maxConcurrent.Should().Be(1, "Concurrent requests for the same key must be strictly serialized.");
        keyedLock.ActiveKeyCount.Should().Be(0, "All semaphores must be cleaned up after use.");
    }

    [Fact]
    public async Task LockAsync_AllowsParallelRequestsForDifferentKeys()
    {
        var keyedLock = new AsyncKeyedLock<long>();
        var startedBarrier = new Barrier(3);
        var tasks = new List<Task>();
        int concurrentCount = 0;
        int maxConcurrent = 0;

        for (int i = 1; i <= 3; i++)
        {
            long key = i;
            tasks.Add(Task.Run(async () =>
            {
                using (await keyedLock.LockAsync(key))
                {
                    int count = Interlocked.Increment(ref concurrentCount);
                    lock (tasks)
                    {
                        if (count > maxConcurrent) maxConcurrent = count;
                    }
                    startedBarrier.SignalAndWait(1000);
                    await Task.Delay(50);
                    Interlocked.Decrement(ref concurrentCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        maxConcurrent.Should().BeGreaterThan(1, "Requests for different keys should run in parallel.");
        keyedLock.ActiveKeyCount.Should().Be(0, "All keys must be cleaned up.");
    }

    [Fact]
    public async Task LockAsync_WithCancellationToken_CleansUpProperly()
    {
        var keyedLock = new AsyncKeyedLock<long>();
        long key = 100;

        // Acquire lock first
        var firstReleaser = await keyedLock.LockAsync(key);
        keyedLock.ActiveKeyCount.Should().Be(1);

        // Second request with token that cancels immediately
        using var cts = new CancellationTokenSource(50);
        Func<Task> act = async () =>
        {
            using (await keyedLock.LockAsync(key, cts.Token))
            {
            }
        };
        await act.Should().ThrowAsync<OperationCanceledException>();

        // First lock is still active
        keyedLock.ActiveKeyCount.Should().Be(1);

        // Release first lock
        firstReleaser.Dispose();

        // Must be completely cleaned up
        keyedLock.ActiveKeyCount.Should().Be(0);
    }

    [Fact]
    public async Task Dispose_IsIdempotent()
    {
        var keyedLock = new AsyncKeyedLock<long>();
        long key = 200;

        var releaser = await keyedLock.LockAsync(key);
        keyedLock.ActiveKeyCount.Should().Be(1);

        releaser.Dispose();
        keyedLock.ActiveKeyCount.Should().Be(0);

        // Multiple dispose calls must not throw or decrement below 0
        var act = () => releaser.Dispose();
        act.Should().NotThrow();
        keyedLock.ActiveKeyCount.Should().Be(0);
    }
}
