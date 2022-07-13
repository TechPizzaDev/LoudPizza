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

        public override uint getAudio(float* aBuffer, uint aSamplesToRead, uint aBufferSize)
        {
            if (mParent.mData == null)
                return 0;

            uint dataleft = mParent.mSampleCount - mOffset;
            uint copylen = dataleft;
            if (copylen > aSamplesToRead)
                copylen = aSamplesToRead;

            for (uint i = 0; i < mChannels; i++)
            {
                CRuntime.memcpy(
                    aBuffer + i * aBufferSize,
                    mParent.mData,
                    sizeof(float) * (mOffset + i * mParent.mSampleCount),
                    sizeof(float) * copylen);
            }

            mOffset += copylen;
            return copylen;
        }

        /// <summary>
        /// Seek to certain place in the buffer.
        /// </summary>
        public override SOLOUD_ERRORS seek(ulong aSamplePosition, float* mScratch, uint mScratchSize)
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

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        public override bool hasEnded()
        {
            return (mFlags & FLAGS.LOOPING) == 0 && mOffset >= mParent.mSampleCount;
        }
    }
}
