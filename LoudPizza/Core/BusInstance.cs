using System;
using System.Runtime.CompilerServices;

namespace LoudPizza.Core
{
    public unsafe class BusInstance : AudioSourceInstance
    {
        public new Bus Source => Unsafe.As<Bus>(base.Source);

        protected uint mScratchSize;
        protected AlignedFloatBuffer mScratch;

        /// <summary>
        /// Approximate volume for channels.
        /// </summary>
        internal ChannelBuffer mVisualizationChannelVolume;

        /// <summary>
        /// Mono-mixed wave data for visualization and for visualization FFT input.
        /// </summary>
        internal Buffer256 mVisualizationWaveData;

        public BusInstance(Bus source) : base(source)
        {
            mFlags |= Flags.Protected | Flags.InaudibleTick;
            mVisualizationChannelVolume = default;
            mVisualizationWaveData = default;
            mScratchSize = SoLoud.SampleGranularity;
            mScratch.init(mScratchSize * SoLoud.MaxChannels);
        }

        public override uint GetAudio(Span<float> aBuffer, uint aSamplesToRead, uint aBufferSize)
        {
            Bus mParent = Source;

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
                SoLoud s = mParent.SoLoud;
                s.mixBus_internal(
                    aBufferPtr, aSamplesToRead, aBufferSize, mScratch.mData, handle, mSamplerate, mChannels, mParent.GetResampler());

                if ((mParent.mFlags & AudioSource.Flags.VisualizationData) != 0)
                {
                    mVisualizationChannelVolume = default;

                    if (aSamplesToRead > 255)
                    {
                        for (uint i = 0; i < 256; i++)
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
                        for (uint i = 0; i < 256; i++)
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

        public override SoLoudStatus Seek(ulong aSamplePosition, Span<float> mScratch)
        {
            return SoLoudStatus.NotImplemented;
        }

        /// <summary>
        /// Busses never stop for fear of going under 50mph.
        /// </summary>
        public override bool HasEnded()
        {
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            Bus mParent = Source;
            SoLoud s = mParent.SoLoud;

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
