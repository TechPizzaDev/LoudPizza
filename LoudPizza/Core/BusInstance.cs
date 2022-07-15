using System;

namespace LoudPizza.Core
{
    public unsafe class BusInstance : AudioSourceInstance
    {
        protected Bus mParent;
        protected uint mScratchSize;
        protected AlignedFloatBuffer mScratch;

        /// <summary>
        /// Approximate volume for channels.
        /// </summary>
        public ChannelBuffer mVisualizationChannelVolume;

        /// <summary>
        /// Mono-mixed wave data for visualization and for visualization FFT input.
        /// </summary>
        public Buffer256 mVisualizationWaveData;

        public BusInstance(Bus aParent)
        {
            mParent = aParent;
            mFlags |= Flags.Protected | Flags.InaudibleTick;
            for (nuint i = 0; i < SoLoud.MaxChannels; i++)
                mVisualizationChannelVolume[i] = 0;
            for (nuint i = 0; i < 256; i++)
                mVisualizationWaveData[i] = 0;
            mScratchSize = SoLoud.SampleGranularity;
            mScratch.init(mScratchSize * SoLoud.MaxChannels);
        }

        public override uint getAudio(Span<float> aBuffer, uint aSamplesToRead, uint aBufferSize)
        {
            uint i;

            Handle handle = mParent.mChannelHandle;
            if (handle.Value == 0)
            {
                // Avoid reuse of scratch data if this bus hasn't played anything yet
                Span<float> slice = aBuffer.Slice(0, (int)(aBufferSize * mChannels));
                slice.Clear();

                return aSamplesToRead;
            }

            fixed (float* aBufferPtr = aBuffer.Slice(0, (int)(aBufferSize * mChannels)))
            {
                SoLoud s = mParent.mSoloud;
                s.mixBus_internal(
                    aBufferPtr, aSamplesToRead, aBufferSize, mScratch.mData, handle, mSamplerate, mChannels, mParent.getResampler());

                if ((mParent.mFlags & AudioSource.Flags.VisualizationData) != 0)
                {
                    for (i = 0; i < SoLoud.MaxChannels; i++)
                        mVisualizationChannelVolume[i] = 0;

                    if (aSamplesToRead > 255)
                    {
                        for (i = 0; i < 256; i++)
                        {
                            mVisualizationWaveData[i] = 0;
                            for (uint j = 0; j < mChannels; j++)
                            {
                                float sample = aBufferPtr[i + aBufferSize * j]; float absvol = MathF.Abs(sample);
                                if (absvol > mVisualizationChannelVolume[j])
                                    mVisualizationChannelVolume[j] = absvol;
                                mVisualizationWaveData[i] += sample;
                            }
                        }
                    }
                    else
                    {
                        // Very unlikely failsafe branch
                        for (i = 0; i < 256; i++)
                        {
                            mVisualizationWaveData[i] = 0;
                            for (uint j = 0; j < mChannels; j++)
                            {
                                float sample = aBufferPtr[(i % aSamplesToRead) + aBufferSize * j];
                                float absvol = MathF.Abs(sample);
                                if (absvol > mVisualizationChannelVolume[j])
                                    mVisualizationChannelVolume[j] = absvol;
                                mVisualizationWaveData[i] += sample;
                            }
                        }
                    }
                }
                return aSamplesToRead;
            }
        }

        public override SoLoudStatus seek(ulong aSamplePosition, Span<float> mScratch)
        {
            return SoLoudStatus.NotImplemented;
        }

        /// <summary>
        /// Busses never stop for fear of going under 50mph.
        /// </summary>
        public override bool hasEnded()
        {
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            SoLoud s = mParent.mSoloud;

            for (uint i = 0; i < s.mHighestVoice; i++)
            {
                AudioSourceInstance? voice = s.mVoice[i];
                if (voice != null && voice.mBusHandle == mParent.mChannelHandle)
                {
                    s.stopVoice_internal(i);
                }
            }

            base.Dispose(disposing);
        }
    }
}
