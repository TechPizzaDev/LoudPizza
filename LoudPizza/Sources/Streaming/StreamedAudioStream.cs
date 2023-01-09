using System;
using System.Collections.Generic;
using System.Threading;
using LoudPizza.Core;

namespace LoudPizza.Sources.Streaming
{
    public class StreamedAudioStream : IAudioStream, IRelativePlaybackRateChangeListener
    {
        private volatile int _disposed;
        private bool _hasEnded;
        private Queue<AudioStreamer.SeekToken> _seekQueue;
        private Queue<AudioStreamer.AudioBuffer> _audioQueue;
        private AudioStreamer.AudioBuffer? _currentBuffer;
        private bool _discardCurrentBuffer;

        public AudioStreamer Streamer { get; }
        public IAudioStream BaseStream { get; }

        public bool NeedsToRead { get; private set; }
        public bool NeedsToSeek => _seekQueue.Count > 0;

        public bool IsDisposed => _disposed != 0;

        /// <inheritdoc/>
        public uint Channels => BaseStream.Channels;

        /// <inheritdoc/>
        public float SampleRate => BaseStream.SampleRate;

        /// <inheritdoc/>
        public float RelativePlaybackSpeed { get; set; }

        public StreamedAudioStream(AudioStreamer streamer, IAudioStream baseStream)
        {
            Streamer = streamer ?? throw new ArgumentNullException(nameof(streamer));
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));

            _seekQueue = new Queue<AudioStreamer.SeekToken>();
            _audioQueue = new Queue<AudioStreamer.AudioBuffer>();
        }

        void IRelativePlaybackRateChangeListener.RelativePlaybackRateChanged(float relativePlaybackSpeed)
        {
            RelativePlaybackSpeed = relativePlaybackSpeed;
        }

        /// <inheritdoc/>
        public bool CanSeek()
        {
            return BaseStream.CanSeek();
        }

        public void ReadWork()
        {
            if (!NeedsToRead)
            {
                return;
            }
            NeedsToRead = false;

            while (_audioQueue.Count < Streamer.ReadBufferCount)
            {
                if (!ReadNewBuffer())
                {
                    break;
                }
            }
        }

        private bool ReadNewBuffer()
        {
            if (_hasEnded)
            {
                return false;
            }

            uint channels = BaseStream.Channels;
            float playbackRate = RelativePlaybackSpeed * BaseStream.SampleRate;
            uint toRead = Math.Max(SoLoud.SampleGranularity, (uint)(playbackRate * Streamer.SecondsPerBuffer));
            AudioStreamer.AudioBuffer audioBuffer = Streamer.RentAudioBuffer(toRead * channels);

            uint samplesRead = BaseStream.GetAudio(audioBuffer.AsSpan(), toRead, toRead);
            if (samplesRead > 0)
            {
                audioBuffer.Start = 0;
                audioBuffer.Length = samplesRead;

                lock (_audioQueue)
                {
                    _audioQueue.Enqueue(audioBuffer);
                }
            }
            else
            {
                Streamer.ReturnAudioBuffer(audioBuffer);

                if (BaseStream.HasEnded())
                {
                    _hasEnded = true;
                    return false;
                }
            }
            return true;
        }

        private void PrimeForMoreAudio()
        {
            if (_audioQueue.Count < Streamer.ReadBufferCount)
            {
                NeedsToRead = true;
                Streamer.NotifyForRead();
            }
        }

        private bool GetCurrentBuffer(out AudioStreamer.AudioBuffer? audioBuffer)
        {
            lock (_audioQueue)
            {
                bool discard = _discardCurrentBuffer;
                _discardCurrentBuffer = false;

                if (_currentBuffer != null)
                {
                    if (_currentBuffer.Start == _currentBuffer.Length)
                    {
                        // The buffer has been consumed, discard it.
                        discard = true;
                    }

                    if (discard)
                    {
                        Streamer.ReturnAudioBuffer(_currentBuffer);
                        _currentBuffer = null;
                    }
                }

                if (_currentBuffer == null)
                {
                    if (!_audioQueue.TryDequeue(out _currentBuffer) && _hasEnded)
                    {
                        audioBuffer = null;
                        return false;
                    }
                }

                audioBuffer = _currentBuffer;
                return true;
            }
        }

        /// <inheritdoc/>
        public uint GetAudio(Span<float> buffer, uint samplesToRead, uint channelStride)
        {
            uint channels = BaseStream.Channels;

            uint totalRead = 0;
            do
            {
                if (!GetCurrentBuffer(out AudioStreamer.AudioBuffer? audioBuffer))
                {
                    // Only return less than the requested amount of samples when the stream ends.
                    return totalRead;
                }

                if (audioBuffer == null)
                {
                    // Fill the rest of the destination with zeroes to pad the total read amount.
                    for (uint i = 0; i < channels; i++)
                    {
                        Span<float> dst = buffer.Slice((int)(totalRead + i * channelStride), (int)samplesToRead);
                        dst.Clear();
                    }

                    totalRead += samplesToRead;
                    samplesToRead = 0;
                    break;
                }

                // C
                uint toCopy = Math.Min(samplesToRead, audioBuffer.Length - audioBuffer.Start);
                ReadOnlySpan<float> totalSrc = audioBuffer.AsSpan();

                for (uint i = 0; i < channels; i++)
                {
                    ReadOnlySpan<float> src = totalSrc.Slice((int)(audioBuffer.Start + i * audioBuffer.Length), (int)toCopy);
                    Span<float> dst = buffer.Slice((int)(totalRead + i * channelStride), src.Length);
                    src.CopyTo(dst);
                }

                audioBuffer.Start += toCopy;
                totalRead += toCopy;
                samplesToRead -= toCopy;
            }
            while (samplesToRead > 0);

            PrimeForMoreAudio();

            return totalRead;
        }

        /// <inheritdoc/>
        public bool HasEnded()
        {
            return _hasEnded;
        }

        public void SeekWork(Span<float> scratch)
        {
            do
            {
                AudioStreamer.SeekToken? token;
                lock (_seekQueue)
                {
                    if (!_seekQueue.TryDequeue(out token))
                    {
                        return;
                    }
                }

                lock (_audioQueue)
                {
                    while (_audioQueue.TryDequeue(out AudioStreamer.AudioBuffer? buffer))
                    {
                        Streamer.ReturnAudioBuffer(buffer);
                    }

                    _discardCurrentBuffer = true;
                }

                try
                {
                    SoLoudStatus status = BaseStream.Seek(
                        token.TargetPosition, scratch, AudioSeekFlags.None, out token.ResultPosition);

                    token.ResultStatus = status;

                    if (status == SoLoudStatus.EndOfStream)
                    {
                        _hasEnded = true;
                    }
                    else if (status == SoLoudStatus.Ok)
                    {
                        _hasEnded = false;

                        if (_seekQueue.Count == 0)
                        {
                            PrimeForMoreAudio();
                        }
                    }
                }
                catch (Exception ex)
                {
                    token.Exception = ex;
                    token.ResultStatus = SoLoudStatus.UnknownError;

                    _hasEnded = true;
                }

                if ((token.Flags & AudioSeekFlags.NonBlocking) == 0)
                {
                    token.WaitHandle.Set();
                }
                else
                {
                    // TODO: bubble up exceptions
                    Streamer.ReturnSeekToken(token);
                }
            }
            while (true);
        }

        /// <inheritdoc/>
        public SoLoudStatus Seek(ulong samplePosition, Span<float> scratch, AudioSeekFlags flags, out ulong resultPosition)
        {
            AudioStreamer.SeekToken token = Streamer.RentSeekToken(samplePosition, flags);
            lock (_seekQueue)
            {
                _seekQueue.Enqueue(token);
            }
            Streamer.NotifyForSeek();

            if ((flags & AudioSeekFlags.NonBlocking) != 0)
            {
                // TODO: try to get stream length from BaseStream and truncate the resultPosition
                resultPosition = samplePosition;

                return SoLoudStatus.Ok;
            }

            token.WaitHandle.Wait();

            resultPosition = token.ResultPosition;
            SoLoudStatus resultStatus = token.ResultStatus;
            Exception? exception = token.Exception;

            Streamer.ReturnSeekToken(token);
            if (exception != null)
            {
                throw exception;
            }
            return resultStatus;
        }

        protected virtual void Dispose(bool disposing)
        {
            int disposed = Interlocked.Exchange(ref _disposed, 1);
            if (disposed != 0)
            {
                return;
            }

            Streamer.UnregisterStream(this);

            if (disposing)
            {
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
