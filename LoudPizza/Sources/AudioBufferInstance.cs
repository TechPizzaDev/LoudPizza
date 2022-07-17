using System;
using System.Runtime.CompilerServices;
using LoudPizza.Core;

namespace LoudPizza.Sources
{
    public unsafe class AudioBufferInstance : AudioSourceInstance
    {
        public new AudioBuffer Source => Unsafe.As<AudioBuffer>(base.Source);

        protected uint mOffset;

        public AudioBufferInstance(AudioBuffer source) : base(source)
        {
            mOffset = 0;
        }

        public override uint GetAudio(Span<float> aBuffer, uint aSamplesToRead, uint aBufferSize)
        {
            if (Source.mData == null)
                return 0;

            uint dataleft = Source.mSampleCount - mOffset;
            uint copylen = dataleft;
            if (copylen > aSamplesToRead)
                copylen = aSamplesToRead;
            int length = (int)copylen;

            for (uint i = 0; i < mChannels; i++)
            {
                Span<float> destination = aBuffer.Slice((int)(i * aBufferSize), length);
                Span<float> source = Source.mData.AsSpan((int)(mOffset + i * Source.mSampleCount), length);

                source.CopyTo(destination);
            }

            mOffset += copylen;
            return copylen;
        }

        /// <summary>
        /// Seek to certain place in the buffer.
        /// </summary>
        public override SoLoudStatus Seek(ulong aSamplePosition, Span<float> mScratch)
        {
            long offset = (long)(aSamplePosition - mStreamPosition);
            if (offset <= 0)
            {
                mOffset = 0;
                mStreamPosition = 0;
                offset = (long)aSamplePosition;
            }
            ulong samples_to_discard = (ulong)offset;

            uint dataleft = Source.mSampleCount - mOffset;
            uint copylen = dataleft;
            if (copylen > samples_to_discard)
                copylen = (uint)samples_to_discard;

            mOffset += copylen;
            mStreamPosition += copylen;

            return SoLoudStatus.Ok;
        }

        public override bool HasEnded()
        {
            return (mFlags & Flags.Looping) == 0 && mOffset >= Source.mSampleCount;
        }
    }
}
