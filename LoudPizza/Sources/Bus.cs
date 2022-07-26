using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using LoudPizza.Core;
using LoudPizza.Modifiers;

namespace LoudPizza.Sources
{
    public unsafe class Bus : AudioSource, IAudioBus
    {
        private BusInstance? mInstance;
        internal Handle mChannelHandle;
        private AudioResampler mResampler;

        public Bus(SoLoud soLoud) : base(soLoud)
        {
            mChannelHandle = default;
            mInstance = null;
            mChannels = 2;
            mResampler = SoLoud.DefaultResampler;
        }

        /// <inheritdoc/>
        public override BusInstance CreateInstance()
        {
            if (mChannelHandle != default)
            {
                Stop();
                mChannelHandle = default;
                mInstance = null;
            }
            mInstance = new BusInstance(this);
            return mInstance;
        }

        /// <inheritdoc/>
        public override void SetFilter(int filterId, Filter? filter)
        {
            base.SetFilter(filterId, filter);

            if (mInstance != null)
            {
                lock (SoLoud.mAudioThreadMutex)
                {
                    mInstance.SetFilter(filterId, filter?.CreateInstance());
                }
            }
        }

        /// <inheritdoc/>
        public VoiceHandle Play(AudioSource source, float volume = -1.0f, float pan = 0.0f, bool paused = false)
        {
            Handle busHandle = GetBusHandle();
            if (busHandle == default)
            {
                return default;
            }

            Handle handle = SoLoud.play(source, volume, pan, paused, busHandle);
            return new VoiceHandle(SoLoud, handle);
        }

        /// <inheritdoc/>
        public VoiceHandle PlayClocked(AudioSource source, Time soundTime, float volume = -1.0f, float pan = 0.0f)
        {
            Handle busHandle = GetBusHandle();
            if (busHandle == default)
            {
                return default;
            }

            Handle handle = SoLoud.playClocked(soundTime, source, volume, pan, busHandle);
            return new VoiceHandle(SoLoud, handle);
        }

        /// <inheritdoc/>
        public VoiceHandle Play3D(
            AudioSource source,
            Vector3 position,
            Vector3 velocity = default,
            float volume = -1.0f,
            bool paused = false)
        {
            Handle busHandle = GetBusHandle();
            if (busHandle == default)
            {
                return default;
            }

            Handle handle = SoLoud.play3d(source, position, velocity, volume, paused, busHandle);
            return new VoiceHandle(SoLoud, handle);
        }

        /// <inheritdoc/>
        public VoiceHandle PlayClocked3D(
            AudioSource source,
            Time soundTime,
            Vector3 position,
            Vector3 velocity = default,
            float volume = -1.0f)
        {
            Handle busHandle = GetBusHandle();
            if (busHandle == default)
            {
                return default;
            }

            Handle handle = SoLoud.play3dClocked(soundTime, source, position, velocity, volume, busHandle);
            return new VoiceHandle(SoLoud, handle);
        }

        /// <inheritdoc/>
        public VoiceHandle PlayBackground(AudioSource source, float volume = 1.0f, bool paused = false)
        {
            Handle busHandle = GetBusHandle();
            if (busHandle == default)
            {
                return default;
            }

            Handle handle = SoLoud.playBackground(source, volume, paused, busHandle);
            return new VoiceHandle(SoLoud, handle);
        }

        /// <summary>
        /// Set number of channels for the bus (default 2).
        /// </summary>
        public SoLoudStatus SetChannels(uint channels)
        {
            if (channels == 0 || channels == 3 || channels == 5 || channels == 7 || channels > SoLoud.MaxChannels)
                return SoLoudStatus.InvalidParameter;

            mChannels = channels;
            return SoLoudStatus.Ok;
        }

        /// <inheritdoc/>
        public void SetVisualizationEnabled(bool enable)
        {
            if (enable)
            {
                mFlags |= Flags.VisualizationData;
            }
            else
            {
                mFlags &= ~Flags.VisualizationData;
            }
        }

        /// <inheritdoc/>
        public bool GetVisualizationEnabled()
        {
            return (mFlags & Flags.VisualizationData) != 0;
        }

        /// <inheritdoc/>
        public void AnnexSound(Handle voiceHandle)
        {
            Handle busHandle = GetBusHandle();

            SoLoud.AnnexSound(voiceHandle, busHandle);
        }

        /// <inheritdoc/>
        [SkipLocalsInit]
        public void CalcFFT(out Buffer256 data)
        {
            float* temp = stackalloc float[1024];

            if (mInstance != null && SoLoud != null)
            {
                lock (SoLoud.mAudioThreadMutex)
                {
                    for (int i = 0; i < 256; i++)
                    {
                        temp[i * 2] = mInstance.mVisualizationWaveData[i];
                        temp[i * 2 + 1] = 0;
                        temp[i + 512] = 0;
                        temp[i + 768] = 0;
                    }
                }

                FFT.fft1024(temp);

                for (int i = 0; i < 256; i++)
                {
                    float real = temp[i * 2];
                    float imag = temp[i * 2 + 1];
                    data[i] = MathF.Sqrt(real * real + imag * imag);
                }
            }
        }

        /// <inheritdoc/>
        public void GetWave(out Buffer256 data)
        {
            if (mInstance != null && SoLoud != null)
            {
                lock (SoLoud.mAudioThreadMutex)
                {
                    data = mInstance.mVisualizationWaveData;
                }
            }
            else
            {
                data = default;
            }
        }

        /// <inheritdoc/>
        public float GetApproximateVolume(uint channel)
        {
            if (channel > mChannels)
                return 0;
            float vol = 0;
            if (mInstance != null && SoLoud != null)
            {
                lock (SoLoud.mAudioThreadMutex)
                {
                    vol = mInstance.mVisualizationChannelVolume[channel];
                }
            }
            return vol;
        }

        /// <inheritdoc/>
        public void GetApproximateVolumes(out ChannelBuffer buffer)
        {
            if (mInstance != null && SoLoud != null)
            {
                lock (SoLoud.mAudioThreadMutex)
                {
                    buffer = mInstance.mVisualizationChannelVolume;
                    return;
                }
            }

            buffer = default;
        }

        /// <inheritdoc/>
        public int GetActiveVoiceCount()
        {
            Handle busHandle = GetBusHandle();
            lock (SoLoud.mAudioThreadMutex)
            {
                int count = 0;
                foreach (AudioSourceInstance? voice in SoLoud.mVoice)
                {
                    if (voice != null && voice.mBusHandle == busHandle)
                        count++;
                }
                return count;
            }
        }

        /// <inheritdoc/>
        public AudioResampler GetResampler()
        {
            return mResampler;
        }

        /// <inheritdoc/>
        public void SetResampler(AudioResampler resampler)
        {
            mResampler = resampler ?? throw new ArgumentNullException(nameof(resampler));
        }

        /// <inheritdoc/>
        public Handle GetBusHandle()
        {
            SoLoud s = SoLoud;
            if (mInstance == null || s == null)
            {
                return default;
            }

            // Find the channel the bus is playing on to calculate handle..
            ReadOnlySpan<AudioSourceInstance?> highVoices = s.mVoice.AsSpan(0, s.mHighestVoice);
            for (int i = 0; mChannelHandle == default && i < highVoices.Length; i++)
            {
                if (highVoices[i] == mInstance)
                {
                    mChannelHandle = s.getHandleFromVoice_internal(i);
                }
            }

            return mChannelHandle;
        }
    }
}