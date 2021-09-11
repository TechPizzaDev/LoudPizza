using System;
using System.Runtime.CompilerServices;
using NLayer;

namespace LoudPizza
{
    public unsafe class Mp3StreamInstance : AudioSourceInstance
    {
        protected Mp3Stream mParent;
        private MpegFile _mpegFile;
        private bool _endOfStream;

        public Mp3StreamInstance(Mp3Stream parent, MpegFile mpegFile)
        {
            mParent = parent ?? throw new ArgumentNullException(nameof(parent));
            _mpegFile = mpegFile ?? throw new ArgumentNullException(nameof(mpegFile));
        }

        [SkipLocalsInit]
        public override uint getAudio(float* aBuffer, uint aSamplesToRead, uint aBufferSize)
        {
            float* localBuffer = stackalloc float[1024];
            Span<float> localSpan = new Span<float>(localBuffer, 1024);

            uint channels = mChannels;
            uint readTarget = aSamplesToRead * channels;
            if ((uint)localSpan.Length > readTarget)
                localSpan = localSpan.Slice(0, (int)readTarget);

            uint samplesRead = (uint)_mpegFile.ReadSamples(localSpan);
            if (samplesRead == 0)
            {
                _endOfStream = true;
                return 0;
            }

            uint elements = samplesRead / channels;

            for (uint i = 0; i < channels; i++)
            {
                for (uint j = 0; j < elements; j++)
                {
                    aBuffer[j + i * aBufferSize] = localBuffer[i + j * channels];
                }
            }

            return elements;
        }

        public override SOLOUD_ERRORS seek(ulong aSamplePosition, float* mScratch, uint mScratchSize)
        {
            long offset = (long)(aSamplePosition - mStreamPosition);
            if (offset <= 0)
            {
                if (rewind() != SOLOUD_ERRORS.SO_NO_ERROR)
                {
                    // can't do generic seek backwards unless we can rewind.
                    return SOLOUD_ERRORS.NOT_IMPLEMENTED;
                }
                offset = (long)aSamplePosition;
            }
            ulong samples_to_discard = (ulong)offset;

            while (samples_to_discard != 0)
            {
                uint samples = mScratchSize / mChannels;
                if (samples > samples_to_discard)
                    samples = (uint)samples_to_discard;

                uint read = getAudio(mScratch, samples, samples);
                if (read == 0)
                    break;
                samples_to_discard -= read;
            }

            mStreamPosition += (ulong)offset;

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        private SOLOUD_ERRORS rewind()
        {
            if (!_mpegFile.CanSeek)
                return SOLOUD_ERRORS.NOT_IMPLEMENTED;

            mStreamPosition = 0;

            _endOfStream = false;
            _mpegFile.Position = 0;

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        public override bool hasEnded()
        {
            return (mFlags & FLAGS.LOOPING) == 0 && _endOfStream;
        }
    }
}
