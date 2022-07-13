using System;
using System.Runtime.CompilerServices;

namespace LoudPizza
{
    public unsafe class Bus : AudioSource
    {
        public BusInstance? mInstance;
        public Handle mChannelHandle;
        public SoLoud.RESAMPLER mResampler;

        public Bus()
        {
            mChannelHandle = default;
            mInstance = null;
            mChannels = 2;
            mResampler = SoLoud.SOLOUD_DEFAULT_RESAMPLER;
        }

        public override BusInstance createInstance()
        {
            if (mChannelHandle.Value != 0)
            {
                stop();
                mChannelHandle = default;
                mInstance = null;
            }
            mInstance = new BusInstance(this);
            return mInstance;
        }

        /// <summary>
        /// Set filter. Set to <see langword="null"/> to clear the filter.
        /// </summary>
        public override void setFilter(uint aFilterId, Filter? aFilter)
        {
            if (aFilterId >= SoLoud.FILTERS_PER_STREAM)
                return;

            mFilter[aFilterId] = aFilter;

            if (mInstance != null)
            {
                lock (mSoloud.mAudioThreadMutex)
                {
                    mInstance.mFilter[aFilterId]?.Dispose();
                    mInstance.mFilter[aFilterId] = null;

                    if (aFilter != null)
                    {
                        mInstance.mFilter[aFilterId] = aFilter.createInstance();
                    }
                }
            }
        }

        /// <summary>
        /// Play sound through the bus.
        /// </summary>
        public Handle play(AudioSource aSound, float aVolume = 1.0f, float aPan = 0.0f, bool aPaused = false)
        {
            if (mInstance == null || mSoloud == null)
            {
                return default;
            }

            findBusHandle();

            if (mChannelHandle.Value == 0)
            {
                return default;
            }
            return mSoloud.play(aSound, aVolume, aPan, aPaused, mChannelHandle);
        }

        /// <summary>
        /// Play sound through the bus, delayed in relation to other sounds called via this function.
        /// </summary>
        public Handle playClocked(Time aSoundTime, AudioSource aSound, float aVolume = 1.0f, float aPan = 0.0f)
        {
            if (mInstance == null || mSoloud == null)
            {
                return default;
            }

            findBusHandle();

            if (mChannelHandle.Value == 0)
            {
                return default;
            }

            return mSoloud.playClocked(aSoundTime, aSound, aVolume, aPan, mChannelHandle);
        }

        /// <summary>
        /// Start playing a 3D audio source through the bus.
        /// </summary>
        public Handle play3d(
            AudioSource aSound,
            float aPosX, float aPosY, float aPosZ,
            float aVelX = 0.0f, float aVelY = 0.0f, float aVelZ = 0.0f,
            float aVolume = 1.0f,
            bool aPaused = false)
        {
            if (mInstance == null || mSoloud == null)
            {
                return default;
            }

            findBusHandle();

            if (mChannelHandle.Value == 0)
            {
                return default;
            }
            return mSoloud.play3d(aSound, aPosX, aPosY, aPosZ, aVelX, aVelY, aVelZ, aVolume, aPaused, mChannelHandle);
        }

        /// <summary>
        /// Start playing a 3D audio source through the bus, delayed in relation to other sounds called via this function.
        /// </summary>
        public Handle play3dClocked(
            Time aSoundTime,
            AudioSource aSound,
            float aPosX, float aPosY, float aPosZ,
            float aVelX = 0.0f, float aVelY = 0.0f, float aVelZ = 0.0f,
            float aVolume = 1.0f)
        {
            if (mInstance == null || mSoloud == null)
            {
                return default;
            }

            findBusHandle();

            if (mChannelHandle.Value == 0)
            {
                return default;
            }
            return mSoloud.play3dClocked(aSoundTime, aSound, aPosX, aPosY, aPosZ, aVelX, aVelY, aVelZ, aVolume, mChannelHandle);
        }

        /// <summary>
        /// Set number of channels for the bus (default 2).
        /// </summary>
        public SOLOUD_ERRORS setChannels(uint aChannels)
        {
            if (aChannels == 0 || aChannels == 3 || aChannels == 5 || aChannels == 7 || aChannels > SoLoud.MAX_CHANNELS)
                return SOLOUD_ERRORS.INVALID_PARAMETER;

            mChannels = aChannels;
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        /// <summary>
        /// Enable or disable visualization data gathering.
        /// </summary>
        public void setVisualizationEnable(bool aEnable)
        {
            if (aEnable)
            {
                mFlags |= FLAGS.VISUALIZATION_DATA;
            }
            else
            {
                mFlags &= ~FLAGS.VISUALIZATION_DATA;
            }
        }

        /// <summary>
        /// Move a live sound to this bus.
        /// </summary>
        public void annexSound(Handle aVoiceHandle)
        {
            findBusHandle();

            void body(Handle h)
            {
                AudioSourceInstance? ch = mSoloud.getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mBusHandle = mChannelHandle;
                }
            }

            lock (mSoloud.mAudioThreadMutex)
            {
                ArraySegment<Handle> h_ = mSoloud.voiceGroupHandleToArray_internal(aVoiceHandle);
                if (h_.Array == null)
                {
                    body(aVoiceHandle);
                }
                else
                {
                    foreach (Handle h in h_.AsSpan())
                        body(h);
                }
            }
        }

        /// <summary>
        /// Calculate and get 256 floats of FFT data for visualization. Visualization has to be enabled before use.
        /// </summary>
        [SkipLocalsInit]
        public void calcFFT(out Buffer256 data)
        {
            float* temp = stackalloc float[1024];

            if (mInstance != null && mSoloud != null)
            {
                lock (mSoloud.mAudioThreadMutex)
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

        /// <summary>
        /// Get 256 floats of wave data for visualization. Visualization has to be enabled before use.
        /// </summary>
        public void getWave(out Buffer256 data)
        {
            if (mInstance != null && mSoloud != null)
            {
                lock (mSoloud.mAudioThreadMutex)
                {
                    data = mInstance.mVisualizationWaveData;
                }
            }
            else
            {
                data = default;
            }
        }

        /// <summary>
        /// Get approximate volume for output channel for visualization. Visualization has to be enabled before use.
        /// </summary>
        public float getApproximateVolume(uint aChannel)
        {
            if (aChannel > mChannels)
                return 0;
            float vol = 0;
            if (mInstance != null && mSoloud != null)
            {
                lock (mSoloud.mAudioThreadMutex)
                {
                    vol = mInstance.mVisualizationChannelVolume[aChannel];
                }
            }
            return vol;
        }

        /// <summary>
        /// Get approximate volumes for all output channels for visualization. Visualization has to be enabled before use.
        /// </summary>
        public ChannelBuffer getApproximateVolumes()
        {
            ChannelBuffer buffer = default;
            if (mInstance != null && mSoloud != null)
            {
                lock (mSoloud.mAudioThreadMutex)
                {
                    buffer = mInstance.mVisualizationChannelVolume;
                }
            }
            return buffer;
        }

        /// <summary>
        /// Get number of immediate child voices to this bus.
        /// </summary>
        public uint getActiveVoiceCount()
        {
            int i;
            uint count = 0;
            findBusHandle();
            lock (mSoloud.mAudioThreadMutex)
            {
                for (i = 0; i < SoLoud.VOICE_COUNT; i++)
                {
                    AudioSourceInstance? voice = mSoloud.mVoice[i];
                    if (voice != null && voice.mBusHandle == mChannelHandle)
                        count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Get current the resampler for this bus.
        /// </summary>
        public SoLoud.RESAMPLER getResampler()
        {
            return mResampler;
        }

        /// <summary>
        /// Set the resampler for this bus.
        /// </summary>
        /// <param name="aResampler"></param>
        public void setResampler(SoLoud.RESAMPLER aResampler)
        {
            if (aResampler <= SoLoud.RESAMPLER.RESAMPLER_CATMULLROM)
                mResampler = aResampler;
        }

        // FFT output data
        //public float mFFTData[256];

        // Snapshot of wave data for visualization
        //public float mWaveData[256];

        /// <summary>
        /// Find the bus' channel.
        /// </summary>
        internal void findBusHandle()
        {
            // Find the channel the bus is playing on to calculate handle..
            for (uint i = 0; mChannelHandle.Value == 0 && i < mSoloud.mHighestVoice; i++)
            {
                if (mSoloud.mVoice[i] == mInstance)
                {
                    mChannelHandle = mSoloud.getHandleFromVoice_internal(i);
                }
            }
        }
    }
}