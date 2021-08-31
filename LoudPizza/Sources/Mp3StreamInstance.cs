using System;
using NLayer;

namespace LoudPizza
{
    public unsafe class Mp3StreamInstance : AudioSourceInstance
    {
        protected Mp3Stream mParent;
        private MpegFile _mpegFile;
        protected uint mOffset;
        private bool _endOfStream;

        public Mp3StreamInstance(Mp3Stream parent, MpegFile mpegFile)
        {
            mParent = parent ?? throw new ArgumentNullException(nameof(parent));
            _mpegFile = mpegFile ?? throw new ArgumentNullException(nameof(mpegFile));
        }

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

            mOffset += elements;
            return elements;
        }

        public override unsafe SOLOUD_ERRORS seek(Time aSeconds, float* mScratch, uint mScratchSize)
        {
            return base.seek(aSeconds, mScratch, mScratchSize);
        }

        public override SOLOUD_ERRORS rewind()
        {
            if (!_mpegFile.CanSeek)
                return SOLOUD_ERRORS.NOT_IMPLEMENTED;

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
