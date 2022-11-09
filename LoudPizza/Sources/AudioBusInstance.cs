using System;
using System.Runtime.CompilerServices;
using LoudPizza.Core;

namespace LoudPizza.Sources
{
    public unsafe class AudioBusInstance : AudioSourceInstance
    {
        public new AudioBus Source => Unsafe.As<AudioBus>(base.Source);

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

        public AudioBusInstance(AudioBus source) : base(source)
        {
            mFlags |= Flags.Protected | Flags.InaudibleTick;
            mVisualizationChannelVolume = default;
            mVisualizationWaveData = default;
            mScratchSize = SoLoud.SampleGranularity;
            mScratch.init(mScratchSize * SoLoud.MaxChannels, SoLoud.VECTOR_SIZE);
        }

        /// <inheritdoc/>
        public override uint GetAudio(Span<float> buffer, uint samplesToRead, uint channelStride)
        {
            AudioBus mParent = Source;
            uint channels = Channels;

            Span<float> bufferSlice = buffer.Slice(0, (int)(channelStride * channels));

            Handle handle = mParent.mChannelHandle;
            if (handle == default)
            {
                // Avoid reuse of scratch data if this bus hasn't played anything yet
                bufferSlice.Clear();

                return samplesToRead;
            }

            SoLoud s = mParent.SoLoud;
            s.mixBus_internal(
                bufferSlice, samplesToRead, channelStride, mScratch.mData, handle, mSamplerate, channels, mParent.GetResampler());

            if ((mParent.mFlags & AudioSource.Flags.VisualizationData) != 0)
            {
                fixed (float* aBufferPtr = bufferSlice)
                {
                    mVisualizationChannelVolume = default;

                    if (samplesToRead > 255)
                    {
                        for (uint i = 0; i < 256; i++)
                        {
                            mVisualizationWaveData[i] = 0;
                            for (uint j = 0; j < channels; j++)
                            {
                                float sample = aBufferPtr[i + channelStride * j]; 
                                float absvol = MathF.Abs(sample);
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
            }
            return samplesToRead;
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
            AudioBus mParent = Source;
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
