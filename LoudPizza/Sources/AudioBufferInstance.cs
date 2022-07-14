using System;

namespace LoudPizza
{
    public unsafe class AudioBufferInstance : AudioSourceInstance
    {
        protected AudioBuffer mParent;
        protected uint mOffset;

        public AudioBufferInstance(AudioBuffer aParent)
        {
            mParent = aParent;
            mOffset = 0;
        }

        public override uint getAudio(Span<float> aBuffer, uint aSamplesToRead, uint aBufferSize)
        {
            if (mParent.mData == null)
                return 0;

            uint dataleft = mParent.mSampleCount - mOffset;
            uint copylen = dataleft;
            if (copylen > aSamplesToRead)
                copylen = aSamplesToRead;

            for (uint i = 0; i < mChannels; i++)
            {
                int length = (int)(sizeof(float) * copylen);
                Span<float> destination = aBuffer.Slice((int)(i * aBufferSize), length);
                Span<float> source = mParent.mData.AsSpan((int)(mOffset + i * mParent.mSampleCount), length);

                source.CopyTo(destination);
            }

            mOffset += copylen;
            return copylen;
        }

        /// <summary>
        /// Seek to certain place in the buffer.
        /// </summary>
        public override SoLoudStatus seek(ulong aSamplePosition, Span<float> mScratch)
        {
            long offset = (long)(aSamplePosition - mStreamPosition);
            if (offset <= 0)
            {
                mOffset = 0;
                mStreamPosition = 0;
                offset = (long)aSamplePosition;
            }
            ulong samples_to_discard = (ulong)offset;

            uint dataleft = mParent.mSampleCount - mOffset;
            uint copylen = dataleft;
            if (copylen > samples_to_discard)
                copylen = (uint)samples_to_discard;

            mOffset += copylen;
            mStreamPosition += copylen;

            return SoLoudStatus.Ok;
        }

        public override bool hasEnded()
        {
            return (mFlags & Flags.Looping) == 0 && mOffset >= mParent.mSampleCount;
        }
    }
}
