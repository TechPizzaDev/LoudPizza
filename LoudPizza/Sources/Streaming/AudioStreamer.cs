using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace LoudPizza.Sources.Streaming
{
    public partial class AudioStreamer
    {
        private enum StreamChangeKind
        {
            Add,
            Remove,
        }

        private readonly record struct StreamChange(StreamChangeKind Kind, StreamedAudioStream Stream);

        private ReadWorker[] _readers;
        private SeekWorker[] _seekers;

        private ReaderWriterLockSlim _streamLock;
        private List<StreamHolder> _streams;
        private ConcurrentQueue<StreamChange> _streamChanges;

        private Queue<SeekToken> _seekTokenPool;
        private Queue<AudioBuffer> _audioBufferPool;
        private ArrayPool<float> _audioBufferArrayPool;

        public int ReadBufferCount { get; set; } = 3;
        public float SecondsPerBuffer { get; set; } = 1 / 12f;

        public AudioStreamer()
        {
            _readers = new ReadWorker[1];
            _seekers = new SeekWorker[1];

            _streamLock = new ReaderWriterLockSlim();
            _streams = new List<StreamHolder>();
            _streamChanges = new ConcurrentQueue<StreamChange>();

            _seekTokenPool = new Queue<SeekToken>();
            _audioBufferPool = new Queue<AudioBuffer>();
            _audioBufferArrayPool = ArrayPool<float>.Create();

            for (int i = 0; i < _readers.Length; i++)
            {
                _readers[i] = new ReadWorker(this);
            }

            for (int i = 0; i < _seekers.Length; i++)
            {
                _seekers[i] = new SeekWorker(this);
            }
        }

        public void Start()
        {
            foreach (ReadWorker worker in _readers)
            {
                worker.Start();
            }

            foreach (SeekWorker worker in _seekers)
            {
                worker.Start();
            }
        }

        private void ProcessStreamChanges()
        {
            if (!_streamChanges.TryDequeue(out StreamChange change))
            {
                return;
            }

            _streamLock.EnterWriteLock();
            try
            {
                do
                {
                    switch (change.Kind)
                    {
                        case StreamChangeKind.Add:
                            _streams.Add(new StreamHolder(change.Stream, new object()));
                            break;

                        case StreamChangeKind.Remove:
                            int index = _streams.IndexOf(new StreamHolder(change.Stream, null!));
                            if (index != -1)
                            {
                                int lastIndex = _streams.Count - 1;
                                _streams[index] = _streams[lastIndex];
                                _streams.RemoveAt(lastIndex);
                            }
                            break;
                    }
                }
                while (_streamChanges.TryDequeue(out change));
            }
            finally
            {
                _streamLock.ExitWriteLock();
            }
        }

        public void RegisterStream(StreamedAudioStream stream)
        {
            _streamChanges.Enqueue(new StreamChange(StreamChangeKind.Add, stream));
        }

        public void UnregisterStream(StreamedAudioStream stream)
        {
            _streamChanges.Enqueue(new StreamChange(StreamChangeKind.Remove, stream));
        }

        public void NotifyForRead()
        {
            foreach (ReadWorker worker in _readers)
            {
                worker.Notify();
            }
        }

        public void NotifyForSeek()
        {
            foreach (SeekWorker worker in _seekers)
            {
                worker.Notify();
            }
        }

        internal SeekToken RentSeekToken(ulong targetSamplePosition, AudioSeekFlags flags)
        {
            SeekToken? token;
            lock (_seekTokenPool)
            {
                _seekTokenPool.TryDequeue(out token);
            }

            if (token != null)
            {
                token.WaitHandle.Reset();
            }
            else
            {
                token = new SeekToken();
            }

            token.TargetPosition = targetSamplePosition;
            token.Flags = flags;

            return token;
        }

        internal void ReturnSeekToken(SeekToken token)
        {
            if (_seekTokenPool.Count < 16)
            {
                token.ResultPosition = default;
                token.ResultStatus = default;
                token.Exception = default;

                lock (_seekTokenPool)
                {
                    _seekTokenPool.Enqueue(token);
                }
            }
            else
            {
                token.WaitHandle.Dispose();
            }
        }

        internal AudioBuffer RentAudioBuffer(uint minimumLength)
        {
            int length = checked((int)minimumLength);

            return new AudioBuffer()
            {
                Buffer = _audioBufferArrayPool.Rent(length)
            };
        }

        internal void ReturnAudioBuffer(AudioBuffer audioBuffer)
        {
            _audioBufferArrayPool.Return(audioBuffer.Buffer);
        }
    }
}
