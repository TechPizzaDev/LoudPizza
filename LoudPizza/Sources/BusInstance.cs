using System;
using System.Runtime.CompilerServices;
using LoudPizza.Core;

namespace LoudPizza.Sources
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

        /// <inheritdoc/>
        public override uint GetAudio(Span<float> buffer, uint samplesToRead, uint channelStride)
        {
            Bus mParent = Source;
            uint channels = Channels;

            Handle handle = mParent.mChannelHandle;
            if (handle == default)
            {
                // Avoid reuse of scratch data if this bus hasn't played anything yet
                Span<float> slice = buffer.Slice(0, (int)(channelStride * channels));
                slice.Clear();

                return samplesToRead;
            }

            fixed (float* aBufferPtr = buffer.Slice(0, (int)(channelStride * channels)))
            {
                SoLoud s = mParent.SoLoud;
                s.mixBus_internal(
                    aBufferPtr, samplesToRead, channelStride, mScratch.mData, handle, mSamplerate, channels, mParent.GetResampler());

                if ((mParent.mFlags & AudioSource.Flags.VisualizationData) != 0)
                {
                    mVisualizationChannelVolume = default;

                    if (samplesToRead > 255)
                    {
                        for (uint i = 0; i < 256; i++)
                        {
                            mVisualizationWaveData[i] = 0;
                            for (uint j = 0; j < channels; j++)
                            {
                                float sample = aBufferPtr[i + channelStride * j]; float absvol = MathF.Abs(sample);
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
                            for (uint j = 0; j < channels; j++)
                            {
                                float sample = aBufferPtr[(i % samplesToRead) + channelStride * j];
                                float absvol = MathF.Abs(sample);
                                if (absvol > mVisualizationChannelVolume[j])
                                    mVisualizationChannelVolume[j] = absvol;
                                mVisualizationWaveData[i] += sample;
                            }
                        }
                    }
                }
                return samplesToRead;
            }
        }

        /// <summary>
        /// Busses are not seekable.
        /// </summary>
        /// <returns>Always <see cref="SoLoudStatus.NotImplemented"/>.</returns>
        public override SoLoudStatus Seek(ulong samplePosition, Span<float> scratch, out ulong resultPosition)
        {
            resultPosition = 0;
            return SoLoudStatus.NotImplemented;
        }

        /// <summary>
        /// Busses never stop for fear of going under 50mph.
        /// </summary>
        public override bool HasEnded()
        {
            return false;
        }

        /// <summary>
        /// Busses are not seekable.
        /// </summary>
        /// <returns>Always <see langword="false"/>.</returns>
        public override bool CanSeek()
        {
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            Bus mParent = Source;
            SoLoud s = mParent.SoLoud;

            ReadOnlySpan<AudioSourceInstance?> highVoices = s.mVoice.AsSpan(0, s.mHighestVoice);
            for (int i = 0; i < highVoices.Length; i++)
            {
                AudioSourceInstance? voice = highVoices[i];
                if (voice != null && voice.mBusHandle == mParent.mChannelHandle)
                {
                    s.stopVoice_internal(i);
                }
            }

            base.Dispose(disposing);
        }
    }
}
