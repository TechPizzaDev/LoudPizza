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

        public override SOLOUD_ERRORS rewind()
        {
            mOffset = 0;
            mStreamPosition = 0.0f;
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        // Seek to certain place in the stream. Base implementation is generic "tape" seek (and slow).
        public override SOLOUD_ERRORS seek(Time aSeconds, float* mScratch, uint mScratchSize)
        {
            if (mParent.mData == null)
                return SOLOUD_ERRORS.SO_NO_ERROR;
            if (aSeconds < 0)
                return SOLOUD_ERRORS.INVALID_PARAMETER;

            Time offset = aSeconds - mStreamPosition;
            if (offset <= 0)
            {
                if (rewind() != SOLOUD_ERRORS.SO_NO_ERROR)
                {
                    // can't do generic seek backwards unless we can rewind.
                    return SOLOUD_ERRORS.NOT_IMPLEMENTED;
                }
                offset = aSeconds;
            }
            uint samples_to_discard = (uint)Math.Floor(mSamplerate * offset);

            uint dataleft = mParent.mSampleCount - mOffset;
            uint copylen = dataleft;
            if (copylen > samples_to_discard)
                copylen = samples_to_discard;

            mOffset += copylen;

            mStreamPosition = offset;
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        public override bool hasEnded()
        {
            return (mFlags & FLAGS.LOOPING) == 0 && mOffset >= mParent.mSampleCount;
        }
    }
}
