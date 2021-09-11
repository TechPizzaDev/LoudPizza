using System;

namespace LoudPizza
{
    // Getters - return information about SoLoud state
    public unsafe partial class SoLoud
    {
        // Query SoLoud version number (should equal to SOLOUD_VERSION macro)
        public uint getVersion()
        {
            return 202002;
        }

        // Get current post-clip scaler value.
        public float getPostClipScaler()
        {
            return mPostClipScaler;
        }

        // Get the current main resampler
        public RESAMPLER getMainResampler()
        {
            return mResampler;
        }

        // Get current global volume
        public float getGlobalVolume()
        {
            return mGlobalVolume;
        }

        // Converts voice + playindex into handle
        internal Handle getHandleFromVoice_internal(uint aVoice)
        {
            AudioSourceInstance? voice = mVoice[aVoice];
            if (voice == null)
                return default;
            return new Handle((aVoice + 1) | (voice.mPlayIndex << 12));
        }

        // Converts handle to voice, if the handle is valid. Returns -1 if not.
        internal int getVoiceFromHandle_internal(Handle aVoiceHandle)
        {
            // If this is a voice group handle, pick the first handle from the group
            ArraySegment<Handle> h = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h.Array != null)
                aVoiceHandle = h[0];

            if (aVoiceHandle.Value == 0)
            {
                return -1;
            }

            int ch = (int)(aVoiceHandle.Value & 0xfff - 1);
            uint idx = aVoiceHandle.Value >> 12;
            AudioSourceInstance? voice = mVoice[ch];
            if (voice != null &&
                (voice.mPlayIndex & 0xfffff) == idx)
            {
                return ch;
            }
            return -1;
        }

        // Converts handle to voice, if the handle is valid. Returns -1 if not.
        internal AudioSourceInstance? getVoiceRefFromHandle_internal(Handle aVoiceHandle)
        {
            // If this is a voice group handle, pick the first handle from the group
            ArraySegment<Handle> h = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h.Array != null)
                aVoiceHandle = h[0];

            if (aVoiceHandle.Value == 0)
            {
                return null;
            }

            int ch = (int)(aVoiceHandle.Value & 0xfff - 1);
            uint idx = aVoiceHandle.Value >> 12;
            AudioSourceInstance? voice = mVoice[ch];
            if (voice != null &&
                (voice.mPlayIndex & 0xfffff) == idx)
            {
                return voice;
            }
            return null;
        }

        // Get current maximum active voice setting
        public uint getMaxActiveVoiceCount()
        {
            return mMaxActiveVoices;
        }

        // Get the current number of busy voices.
        public uint getActiveVoiceCount()
        {
            lockAudioMutex_internal();
            if (mActiveVoiceDirty)
                calcActiveVoices_internal();
            uint c = mActiveVoiceCount;
            unlockAudioMutex_internal();
            return c;
        }

        // Get the current number of voices in SoLoud
        public uint getVoiceCount()
        {
            lockAudioMutex_internal();
            uint c = 0;
            for (uint i = 0; i < mHighestVoice; i++)
            {
                if (mVoice[i] != null)
                {
                    c++;
                }
            }
            unlockAudioMutex_internal();
            return c;
        }

        // Check if the handle is still valid, or if the sound has stopped.
        public bool isValidVoiceHandle(Handle aVoiceHandle)
        {
            // voice groups are not valid voice handles
            if ((aVoiceHandle.Value & 0xfffff000) == 0xfffff000)
                return false;

            lockAudioMutex_internal();
            if (getVoiceFromHandle_internal(aVoiceHandle) != -1)
            {
                unlockAudioMutex_internal();
                return true;
            }
            unlockAudioMutex_internal();
            return false;
        }

        // Get voice loop point value
        public Time getLoopPoint(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            Time v = ch.mLoopPoint;
            unlockAudioMutex_internal();
            return v;
        }

        // Query whether a voice is set to loop.
        public bool getLooping(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            var ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return false;
            }
            var v = ch.mFlags & AudioSourceInstance.FLAGS.LOOPING;
            unlockAudioMutex_internal();
            return v != 0;
        }

        // Query whether a voice is set to auto-stop when it ends.
        public bool getAutoStop(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return false;
            }
            var v = ch.mFlags & AudioSourceInstance.FLAGS.DISABLE_AUTOSTOP;
            unlockAudioMutex_internal();
            return v == 0;
        }

        // Get audiosource-specific information from a voice.
        public float getInfo(Handle aVoiceHandle, uint mInfoKey)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            float v = ch.getInfo(mInfoKey);
            unlockAudioMutex_internal();
            return v;
        }

        // Get current volume.
        public float getVolume(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            var ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            float v = ch.mSetVolume;
            unlockAudioMutex_internal();
            return v;
        }

        // Get current overall volume (set volume * 3d volume)
        public float getOverallVolume(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            float v = ch.mOverallVolume;
            unlockAudioMutex_internal();
            return v;
        }

        // Get current pan.
        public float getPan(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            float v = ch.mPan;
            unlockAudioMutex_internal();
            return v;
        }

        // Get current play time, in seconds.
        public Time getStreamTime(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            double v = ch.mStreamTime;
            unlockAudioMutex_internal();
            return v;
        }

        // Get current stream position in samples.
        public ulong getStreamSamplePosition(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            ulong v = ch.mStreamPosition;
            unlockAudioMutex_internal();
            return v;
        }

        // Get current stream position in seconds.
        public Time getStreamTimePosition(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            ulong pos = ch.mStreamPosition;
            float rate = ch.mSamplerate;
            unlockAudioMutex_internal();
            return pos / (double)rate;
        }

        // Get current relative play speed.
        public float getRelativePlaySpeed(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 1;
            }
            float v = ch.mSetRelativePlaySpeed;
            unlockAudioMutex_internal();
            return v;
        }

        // Get current sample rate.
        public float getSamplerate(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            float v = ch.mBaseSamplerate;
            unlockAudioMutex_internal();
            return v;
        }

        // Get current pause state.
        public bool getPause(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return false;
            }
            var v = ch.mFlags & AudioSourceInstance.FLAGS.PAUSED;
            unlockAudioMutex_internal();
            return v != 0;
        }

        // Get current voice protection state.
        public bool getProtectVoice(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return false;
            }
            var v = ch.mFlags & AudioSourceInstance.FLAGS.PROTECTED;
            unlockAudioMutex_internal();
            return v != 0;
        }

        // Find a free voice, stopping the oldest if no free voice is found.
        internal int findFreeVoice_internal()
        {
            uint lowest_play_index_value = 0xffffffff;
            int lowest_play_index = -1;

            // (slowly) drag the highest active voice index down
            if (mHighestVoice > 0 && mVoice[mHighestVoice - 1] == null)
                mHighestVoice--;

            for (int i = 0; i < VOICE_COUNT; i++)
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
                if (((voice.mFlags & AudioSourceInstance.FLAGS.PROTECTED) == 0) &&
                    voice.mPlayIndex < lowest_play_index_value)
                {
                    lowest_play_index_value = voice.mPlayIndex;
                    lowest_play_index = i;
                }
            }
            stopVoice_internal((uint)lowest_play_index);
            return lowest_play_index;
        }

        // Get current loop count. Returns 0 if handle is not valid. (All audio sources may not update loop count)
        public uint getLoopCount(Handle aVoiceHandle)
        {
            lockAudioMutex_internal();
            AudioSourceInstance? ch = getVoiceRefFromHandle_internal(aVoiceHandle);
            if (ch == null)
            {
                unlockAudioMutex_internal();
                return 0;
            }
            uint v = ch.mLoopCount;
            unlockAudioMutex_internal();
            return v;
        }

        // Returns current backend ID (BACKENDS enum)
        public BACKENDS getBackendId()
        {
            return mBackendID;
        }

        // Returns current backend string. May be NULL.
        public string? getBackendString()
        {
            return mBackendString;
        }

        // Returns current backend channel count (1 mono, 2 stereo, etc)
        public uint getBackendChannels()
        {
            return mChannels;
        }

        // Returns current backend sample rate
        public uint getBackendSamplerate()
        {
            return mSamplerate;
        }

        // Returns current backend buffer size
        public uint getBackendBufferSize()
        {
            return mBufferSize;
        }

        // Get speaker position in 3d space
        public SOLOUD_ERRORS getSpeakerPosition(uint aChannel, out float aX, out float aY, out float aZ)
        {
            if (aChannel >= mChannels)
            {
                aX = 0f;
                aY = 0f;
                aZ = 0f;
                return SOLOUD_ERRORS.INVALID_PARAMETER;
            }
            Vec3 position = m3dSpeakerPosition[aChannel];
            aX = position.X;
            aY = position.Y;
            aZ = position.Z;
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }
    }
}
