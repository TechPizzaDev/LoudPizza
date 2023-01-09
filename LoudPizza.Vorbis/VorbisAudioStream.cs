using System;
using LoudPizza.Sources;
using NVorbis;

namespace LoudPizza.Vorbis
{
    public class VorbisAudioStream : IAudioStream
    {
        public bool IsDisposed { get; private set; }
        public VorbisReader Reader { get; private set; }

        public uint Channels => (uint)Reader.Channels;

        public float SampleRate => Reader.SampleRate;

        public float RelativePlaybackSpeed => 1;

        public VorbisAudioStream(VorbisReader vorbisReader)
        {
            Reader = vorbisReader ?? throw new ArgumentNullException(nameof(vorbisReader));
        }

        /// <inheritdoc/>
        public uint GetAudio(Span<float> buffer, uint samplesToRead, uint channelStride)
        {
            int sampleCount = Reader.ReadSamples(buffer, (int)samplesToRead, (int)channelStride);
            return (uint)sampleCount;
        }

        /// <inheritdoc/>
        public bool HasEnded()
        {
            return Reader.IsEndOfStream;
        }

        /// <inheritdoc/>
        public bool CanSeek()
        {
            return Reader.CanSeek;
        }

        /// <inheritdoc/>
        public SoLoudStatus Seek(ulong samplePosition, Span<float> scratch, AudioSeekFlags flags, out ulong resultPosition)
        {
            long signedSamplePosition = (long)samplePosition;
            if (signedSamplePosition < 0)
            {
                resultPosition = (ulong)Reader.TotalSamples;
                return SoLoudStatus.EndOfStream;
            }

            // TODO: bubble up exceptions?
            try
            {
                Reader.SeekTo(signedSamplePosition);
                resultPosition = (ulong)Reader.SamplePosition;
                return SoLoudStatus.Ok;
            }
            catch (PreRollPacketException)
            {
                resultPosition = (ulong)Reader.SamplePosition;
                return SoLoudStatus.FileLoadFailed;
            }
            catch (SeekOutOfRangeException)
            {
                resultPosition = (ulong)Reader.TotalSamples;
                return SoLoudStatus.EndOfStream;
            }
            catch (Exception)
            {
                resultPosition = 0;
                return SoLoudStatus.UnknownError;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Reader.Dispose();
                    Reader = null!;
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
