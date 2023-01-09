using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace LoudPizza.Sources.Streaming
{
    public partial class AudioStreamer
    {
        private abstract class Worker : IDisposable
        {
            private Thread _thread;
            private ManualResetEventSlim _resetEvent;
            private bool _disposed;

            public AudioStreamer Streamer { get; }

            public Worker(AudioStreamer streamer)
            {
                Streamer = streamer ?? throw new ArgumentNullException(nameof(streamer));

                _thread = new Thread(WorkerThread)
                {
                    IsBackground = true
                };
                _resetEvent = new ManualResetEventSlim(false, 0);
            }

            protected abstract bool ShouldWork(StreamedAudioStream stream);

            protected abstract void Work(StreamedAudioStream stream);

            public void Start()
            {
                _thread.Start();
            }

            public void Notify()
            {
                // We do not need a pulse for every call;
                // checking is cheap and being set means the worker will run soon regardless.
                if (!_resetEvent.IsSet)
                {
                    _resetEvent.Set();
                }
            }

            private void WorkerThread()
            {
                Stopwatch watch = new();
                List<StreamHolder> holders = new();

                while (true)
                {
                    _resetEvent.Wait();
                    _resetEvent.Reset();

                    watch.Restart();

                    Streamer.ProcessStreamChanges();

                    Streamer._streamLock.EnterReadLock();
                    try
                    {
                        foreach (ref StreamHolder holder in CollectionsMarshal.AsSpan(Streamer._streams))
                        {
                            if (ShouldWork(holder.Stream))
                            {
                                holders.Add(holder);
                            }
                        }
                    }
                    finally
                    {
                        Streamer._streamLock.ExitReadLock();
                    }

                    foreach (ref StreamHolder holder in CollectionsMarshal.AsSpan(holders))
                    {
                        // TryEnter allows other workers to steal work
                        if (Monitor.TryEnter(holder.Mutex))
                        {
                            try
                            {
                                Work(holder.Stream);
                            }
                            finally
                            {
                                Monitor.Exit(holder.Mutex);
                            }
                        }
                    }
                    holders.Clear();

                    watch.Stop();
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _resetEvent.Dispose();
                    }

                    _disposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
