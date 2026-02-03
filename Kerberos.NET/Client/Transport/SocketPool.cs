// -----------------------------------------------------------------------
// Licensed to The .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kerberos.NET.Dns;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace Kerberos.NET.Client
{
    internal class SocketPool : ISocketPool
    {
        private readonly ConcurrentDictionary<string, NamedPool> pool
            = new ConcurrentDictionary<string, NamedPool>();

        //private readonly Task backgroundWorker;
        private readonly CancellationTokenSource cts;

        public SocketPool()
        {
            this.cts = new CancellationTokenSource();

            //this.backgroundWorker = Task.Run(() => this.PollPool(), this.cts.Token);
        }

        public int MaxPoolSize { get; set; } = 10;

        public TimeSpan ScavengeWindow { get; set; } = TimeSpan.FromSeconds(5);//);

        public async Task<ITcpSocket> Request(DnsRecord target, TimeSpan connectTimeout)
        {
            if (!this.pool.TryGetValue(target.Address, out NamedPool queue))
            {
                queue = new NamedPool(target) { MaxPoolSize = this.MaxPoolSize };

                var watch = Stopwatch.StartNew();
                this.pool.TryAdd(target.Address, queue);
                watch.Stop();
                Console.WriteLine($"||||||||||||||||||||||||||----------------------> this.pool.TryAdd(target.Address, queue);: {watch.ElapsedMilliseconds}ms");
            }

            if (queue.Queue.TryDequeue(out TcpSocket client))
            {
                var watch = Stopwatch.StartNew();
                if (client.GetTcpClientState() == TcpState.Established)
                {
                    return client;
                }
                client.Free();
                queue.Release(client);
                watch.Stop();
                Console.WriteLine($"##############----------------------> GetTcpClientState(): {watch.ElapsedMilliseconds}ms");
                Console.WriteLine($"----------------------> TryDequeue: ");
            }

            return await queue.OpenSocket(connectTimeout).ConfigureAwait(false);
        }

        private async Task PollPool()
        {
            while (!this.cts.Token.IsCancellationRequested)
            {
                foreach (var key in this.pool.Keys.ToList())
                {
                    var queue = this.pool[key];

                    queue.Poll(this.ScavengeWindow);
                }

                await Task.Delay(this.ScavengeWindow, this.cts.Token).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"SOCKET POOL DISPOSED --------------------------------");
            this.cts.Cancel();
            this.cts.Dispose();
            //this.backgroundWorker.ContinueWith(t => t.Dispose(), TaskScheduler.Default);

            foreach (var key in this.pool.Keys.ToList())
            {
                var queue = this.pool[key];

                queue.Dispose();
            }
        }
    }
}
