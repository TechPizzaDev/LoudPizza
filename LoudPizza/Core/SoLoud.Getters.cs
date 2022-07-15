using System;
using System.Numerics;

namespace LoudPizza.Core
{
    // Getters - return information about SoLoud state
    public unsafe partial class SoLoud
    {
        /// <summary>
        /// Query the version number (should equal to SOLOUD_VERSION macro).
        /// </summary>
        public uint getVersion()
        {
            return 202002;
        }

        /// <summary>
        /// Get current post-clip scaler value.
        /// </summary>
        public float getPostClipScaler()
        {
            return mPostClipScaler;
        }

        /// <summary>
        /// Get the current main resampler.
        /// </summary>
        public AudioResampler getMainResampler()
        {
            return mResampler;
        }

        /// <summary>
        /// Get current global volume.
        /// </summary>
        public float getGlobalVolume()
        {
            return mGlobalVolume;
        }

        /// <summary>
        /// Converts voice + playindex into handle.
        /// </summary>
        internal Handle getHandleFromVoice_internal(uint aVoice)
        {
            AudioSourceInstance? voice = mVoice[aVoice];
            if (voice == null)
                return default;
            return new Handle((aVoice + 1) | (voice.mPlayIndex << 12));
        }

        /// <summary>
        /// Converts handle to voice, if the handle is valid. Returns -1 if not.
        /// </summary>
        internal int getVoiceFromHandle_internal(Handle aVoiceHandle)
        {
            // If this is a voice group handle, pick the first handle from the group
            ReadOnlySpan<Handle> h = VoiceGroupHandleToSpan(ref aVoiceHandle);
            Handle handle = h[0];

            if (handle.Value == 0)
            {
                return -1;
            }

            int ch = (int)(handle.Value & 0xfff - 1);
            uint idx = handle.Value >> 12;
            AudioSourceInstance? voice = mVoice[ch];
            if (voice != null &&
                (voice.mPlayIndex & 0xfffff) == idx)
            {
                return ch;
            }
            return -1;
        }

        /// <summary>
        /// Converts handle to voice, if the handle is valid. Returns -1 if not.
        /// </summary>
        internal AudioSourceInstance? getVoiceRefFromHandle_internal(Handle aVoiceHandle)
        {
            // If this is a voice group handle, pick the first handle from the group
            ReadOnlySpan<Handle> h = VoiceGroupHandleToSpan(ref aVoiceHandle);
            Handle handle = h[0];

            if (handle.Value == 0)
            {
                return null;
            }

            int ch = (int)(handle.Value & 0xfff - 1);
            uint idx = handle.Value >> 12;
            AudioSourceInstance? voice = mVoice[ch];
            if (voice != null &&
                (voice.mPlayIndex & 0xfffff) == idx)
            {
                return voice;
            }
            return null;
        }

        /// <summary>
        /// Get current maximum active voice setting.
        /// </summary>
        public uint getMaxActiveVoiceCount()
        {
            return mMaxActiveVoices;
        }

        /// <summary>
        /// Get the current number of busy voices.
        /// </summary>
        public uint getActiveVoiceCount()
        {
            lock (mAudioThreadMutex)
            {
                if (mActiveVoiceDirty)
                    calcActiveVoices_internal();
                uint c = mActiveVoiceCount;
                return c;
            }
        }

        /// <summary>
        /// Get the current number of voices.
        /// </summary>
        public uint getVoiceCount()
        {
            lock (mAudioThreadMutex)
            {
                uint c = 0;
                for (uint i = 0; i < mHighestVoice; i++)
                {
                    if (mVoice[i] != null)
                    {
                        c++;
                    }
                }
                return c;
            }
        }

        /// <summary>
        /// Check if the handle is still valid, or if the sound has stopped.
        /// </summary>
        public bool isValidVoiceHandle(Handle aVoiceHandle)
        {
            // voice groups are not valid voice handles
            if ((aVoiceHandle.Value & 0xfffff000) == 0xfffff000)
                return false;

            lock (mAudioThreadMutex)
            {
                bool result = getVoiceFromHandle_internal(aVoiceHandle) != -1;
                return result;
            }
        }

        /// <summary>
        /// Get voice loop point value.
        /// </summary>
        public Time getLoopPoint(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                Time v = ch.mLoopPoint;
                return v;
            }
        }

        /// <summary>
        /// Query whether a voice is set to loop.
        /// </summary>
        public bool getLooping(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return false;
                }
                AudioSourceInstance.Flags v = ch.mFlags & AudioSourceInstance.Flags.Looping;
                return v != 0;
            }
        }

        /// <summary>
        /// Query whether a voice is set to auto-stop when it ends.
        /// </summary>
        public bool getAutoStop(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return false;
                }
                AudioSourceInstance.Flags v = ch.mFlags & AudioSourceInstance.Flags.DisableAutostop;
                return v == 0;
            }
        }

        /// <summary>
        /// Get audiosource-specific information from a voice.
        /// </summary>
        public float getInfo(Handle aVoiceHandle, uint mInfoKey)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                float v = ch.getInfo(mInfoKey);
                return v;
            }
        }

        /// <summary>
        /// Get current volume.
        /// </summary>
        public float getVolume(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                float v = ch.mSetVolume;
                return v;
            }
        }

        /// <summary>
        /// Get current overall volume (set volume * 3D volume).
        /// </summary>
        public float getOverallVolume(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                float v = ch.mOverallVolume;
                return v;
            }
        }

        /// <summary>
        /// Get current pan.
        /// </summary>
        public float getPan(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                float v = ch.mPan;
                return v;
            }
        }

        /// <summary>
        /// Get current play time.
        /// </summary>
        public Time getStreamTime(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                Time v = ch.mStreamTime;
                return v;
            }
        }

        /// <summary>
        /// Get current stream position in samples.
        /// </summary>
        public ulong getStreamSamplePosition(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                ulong v = ch.mStreamPosition;
                return v;
            }
        }

        /// <summary>
        /// Get current stream position in seconds.
        /// </summary>
        public Time getStreamTimePosition(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                ulong pos = ch.mStreamPosition;
                float rate = ch.mSamplerate;
                return pos / (double)rate;
            }
        }

        /// <summary>
        /// Get current relative play speed.
        /// </summary>
        public float getRelativePlaySpeed(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 1;
                }
                float v = ch.mSetRelativePlaySpeed;
                return v;
            }
        }

        /// <summary>
        /// Get current sample rate.
        /// </summary>
        public float getSamplerate(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                float v = ch.mBaseSamplerate;
                return v;
            }
        }

        /// <summary>
        /// Get current pause state.
        /// </summary>
        public bool getPause(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return false;
                }
                AudioSourceInstance.Flags v = ch.mFlags & AudioSourceInstance.Flags.Paused;
                return v != 0;
            }
        }

        /// <summary>
        /// Get current voice protection state.
        /// </summary>
        public bool getProtectVoice(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return false;
                }
                AudioSourceInstance.Flags v = ch.mFlags & AudioSourceInstance.Flags.Protected;
                return v != 0;
            }
        }

        /// <summary>
        /// Find a free voice, stopping the oldest if no free voice is found.
        /// </summary>
        internal int findFreeVoice_internal()
        {
            uint lowest_play_index_value = 0xffffffff;
            int lowest_play_index = -1;

            // (slowly) drag the highest active voice index down
            if (mHighestVoice > 0 && mVoice[mHighestVoice - 1] == null)
                mHighestVoice--;

            for (int i = 0; i < MaxVoiceCount; i++)
            {
                AudioSourceInstance? voice = mVoice[i];
                if (voice == null)
                {
                    if (i + 1 > mHighestVoice)
                    {
                        mHighestVoice = (uint)(i + 1);
                    }
                    return i;
                }
                if (((voice.mFlags & AudioSourceInstance.Flags.Protected) == 0) &&
                    voice.mPlayIndex < lowest_play_index_value)
                {
                    lowest_play_index_value = voice.mPlayIndex;
                    lowest_play_index = i;
                }
            }
            stopVoice_internal((uint)lowest_play_index);
            return lowest_play_index;
        }

        /// <summary>
        /// Get current loop count. Returns 0 if handle is not valid. 
        /// </summary>
        /// <remarks>
        /// All audio sources may not update loop count.
        /// </remarks>
        public uint getLoopCount(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
                if (ch == null)
                {
                    return 0;
                }
                uint v = ch.mLoopCount;
                return v;
            }
        }

        /// <summary>
        /// Returns current backend string. May be <see langword="null"/>.
        /// </summary>
        public string? getBackendString()
        {
            return mBackendString;
        }

        /// <summary>
        /// Returns current backend channel count (1 mono, 2 stereo, etc).
        /// </summary>
        public uint getBackendChannels()
        {
            return mChannels;
        }

        /// <summary>
        /// Returns current backend sample rate.
        /// </summary>
        public uint getBackendSamplerate()
        {
            return mSamplerate;
        }

        /// <summary>
        /// Returns current backend buffer size.
        /// </summary>
        public uint getBackendBufferSize()
        {
            return mBufferSize;
        }

        /// <summary>
        /// Get speaker position in 3D space.
        /// </summary>
        public SoLoudStatus getSpeakerPosition(uint aChannel, out Vector3 aPosition)
        {
            if (aChannel >= mChannels)
            {
                aPosition = default;
                return SoLoudStatus.InvalidParameter;
            }
            Vector3 position = m3dSpeakerPosition[aChannel];
            aPosition = position;
            return SoLoudStatus.Ok;
        }
    }
}
