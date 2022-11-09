using System;
using System.Runtime.CompilerServices;

namespace LoudPizza.Sources
{
    public class AudioQueueInstance : AudioSourceInstance
    {
        public new AudioQueue Source => Unsafe.As<AudioQueue>(base.Source);

        public AudioQueueInstance(AudioQueue source) : base(source)
        {
            mFlags |= Flags.Protected;
        }

        /// <inheritdoc/>
        public override uint GetAudio(Span<float> buffer, uint samplesToRead, uint channelStride)
        {
            AudioQueue parent = Source;
            if (parent.mCount == 0)
            {
                return 0;
            }

            uint copycount = samplesToRead;
            uint copyofs = 0;
            while (copycount != 0 && parent.mCount != 0)
            {
                IAudioStream source = parent.mSource[parent.mReadIndex]!;
                uint readcount = source.GetAudio(buffer.Slice((int)copyofs), copycount, channelStride);
                copyofs += readcount;
                copycount -= readcount;
                if (source.HasEnded())
                {
                    source.Dispose();
                    parent.mSource[parent.mReadIndex] = null;
                    parent.mReadIndex = (parent.mReadIndex + 1) % (uint)parent.mSource.Length;
                    parent.mCount--;
                    mLoopCount++;
                }
            }
            return copyofs;
        }

        /// <inheritdoc/>
        public override SoLoudStatus Seek(ulong aSamplePosition, Span<float> mScratch, out ulong resultPosition)
        {
            resultPosition = 0;
            return SoLoudStatus.NotImplemented;
        }

        /// <inheritdoc/>
        public override bool HasEnded()
        {
            return mLoopCount != 0 && Source.mCount == 0;
        }

        /// <inheritdoc/>
        public override bool CanSeek()
        {
            return false;
        }
    }
}
