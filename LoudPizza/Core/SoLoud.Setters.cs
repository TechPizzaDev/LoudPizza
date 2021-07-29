using System;

namespace LoudPizza
{
    // Setters - set various bits of SoLoud state
    public unsafe partial class SoLoud
    {
        // Set the post clip scaler value
        public void setPostClipScaler(float aScaler)
        {
            mPostClipScaler = aScaler;
        }

        // Set the main resampler
        public void setMainResampler(RESAMPLER aResampler)
        {
            if (aResampler <= RESAMPLER.RESAMPLER_CATMULLROM)
                mResampler = aResampler;
        }

        // Set the global volume
        public void setGlobalVolume(float aVolume)
        {
            mGlobalVolumeFader.mActive = 0;
            mGlobalVolume = aVolume;
        }

        // Set the relative play speed
        public SOLOUD_ERRORS setRelativePlaySpeed(Handle aVoiceHandle, float aSpeed)
        {
            SOLOUD_ERRORS retVal = 0;

            void body(Handle h)
            {
                int ch = getVoiceFromHandle_internal(h);
                if (ch != -1)
                {
                    mVoice[ch]!.mRelativePlaySpeedFader.mActive = 0;
                    retVal = setVoiceRelativePlaySpeed_internal((uint)ch, aSpeed);
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();

            return retVal;
        }

        // Set the sample rate
        public void setSamplerate(Handle aVoiceHandle, float aSamplerate)
        {
            void body(Handle h)
            {
                int ch = getVoiceFromHandle_internal(h);
                if (ch != -1)
                {
                    mVoice[ch]!.mBaseSamplerate = aSamplerate;
                    updateVoiceRelativePlaySpeed_internal((uint)ch);
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set the pause state
        public void setPause(Handle aVoiceHandle, bool aPause)
        {
            void body(Handle h)
            {
                int ch = getVoiceFromHandle_internal(h);
                if (ch != -1)
                {
                    setVoicePause_internal((uint)ch, aPause);
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set current maximum active voice setting
        public SOLOUD_ERRORS setMaxActiveVoiceCount(uint aVoiceCount)
        {
            if (aVoiceCount == 0 || aVoiceCount >= VOICE_COUNT)
                return SOLOUD_ERRORS.INVALID_PARAMETER;
            lockAudioMutex_internal();
            mMaxActiveVoices = aVoiceCount;
            initResampleData();
            mActiveVoiceDirty = true;
            unlockAudioMutex_internal();
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        // Pause all voices
        public void setPauseAll(bool aPause)
        {
            lockAudioMutex_internal();
            for (uint ch = 0; ch < mHighestVoice; ch++)
            {
                setVoicePause_internal(ch, aPause);
            }
            unlockAudioMutex_internal();
        }

        // Set the voice protection state
        public void setProtectVoice(Handle aVoiceHandle, bool aProtect)
        {
            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    if (aProtect)
                    {
                        ch.mFlags |= AudioSourceInstance.FLAGS.PROTECTED;
                    }
                    else
                    {
                        ch.mFlags &= ~AudioSourceInstance.FLAGS.PROTECTED;
                    }
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set panning value; -1 is left, 0 is center, 1 is right
        public void setPan(Handle aVoiceHandle, float aPan)
        {
            void body(Handle h)
            {
                int ch = getVoiceFromHandle_internal(h);
                if (ch != -1)
                {
                    setVoicePan_internal((uint)ch, aPan);
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set channel volume (volume for a specific speaker)
        public void setChannelVolume(Handle aVoiceHandle, uint aChannel, float aVolume)
        {
            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    if (ch.mChannels > aChannel)
                    {
                        ch.mChannelVolume[aChannel] = aVolume;
                    }
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set absolute left/right volumes
        public void setPanAbsolute(Handle aVoiceHandle, float aLVolume, float aRVolume)
        {
            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mPanFader.mActive = 0;
                    ch.mChannelVolume[0] = aLVolume;
                    ch.mChannelVolume[1] = aRVolume;

                    if (ch.mChannels == 4)
                    {
                        ch.mChannelVolume[2] = aLVolume;
                        ch.mChannelVolume[3] = aRVolume;
                    }
                    else if (ch.mChannels == 6)
                    {
                        ch.mChannelVolume[2] = (aLVolume + aRVolume) * 0.5f;
                        ch.mChannelVolume[3] = (aLVolume + aRVolume) * 0.5f;
                        ch.mChannelVolume[4] = aLVolume;
                        ch.mChannelVolume[5] = aRVolume;
                    }
                    else if (ch.mChannels == 8)
                    {
                        ch.mChannelVolume[2] = (aLVolume + aRVolume) * 0.5f;
                        ch.mChannelVolume[3] = (aLVolume + aRVolume) * 0.5f;
                        ch.mChannelVolume[4] = aLVolume;
                        ch.mChannelVolume[5] = aRVolume;
                        ch.mChannelVolume[6] = aLVolume;
                        ch.mChannelVolume[7] = aRVolume;
                    }
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set behavior for inaudible sounds
        public void setInaudibleBehavior(Handle aVoiceHandle, bool aMustTick, bool aKill)
        {
            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mFlags &= ~(AudioSourceInstance.FLAGS.INAUDIBLE_KILL | AudioSourceInstance.FLAGS.INAUDIBLE_TICK);
                    if (aMustTick)
                    {
                        ch.mFlags |= AudioSourceInstance.FLAGS.INAUDIBLE_TICK;
                    }
                    if (aKill)
                    {
                        ch.mFlags |= AudioSourceInstance.FLAGS.INAUDIBLE_KILL;
                    }
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set voice loop point value
        public void setLoopPoint(Handle aVoiceHandle, Time aLoopPoint)
        {
            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mLoopPoint = aLoopPoint;
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set voice's loop state
        public void setLooping(Handle aVoiceHandle, bool aLooping)
        {
            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    if (aLooping)
                    {
                        ch.mFlags |= AudioSourceInstance.FLAGS.LOOPING;
                    }
                    else
                    {
                        ch.mFlags &= ~AudioSourceInstance.FLAGS.LOOPING;
                    }
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set whether sound should auto-stop when it ends
        public void setAutoStop(Handle aVoiceHandle, bool aAutoStop)
        {
            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    if (aAutoStop)
                    {
                        ch.mFlags &= ~AudioSourceInstance.FLAGS.DISABLE_AUTOSTOP;
                    }
                    else
                    {
                        ch.mFlags |= AudioSourceInstance.FLAGS.DISABLE_AUTOSTOP;
                    }
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set overall volume
        public void setVolume(Handle aVoiceHandle, float aVolume)
        {
            void body(Handle h)
            {
                int ch = getVoiceFromHandle_internal(h);
                if (ch != -1)
                {
                    mVoice[ch]!.mVolumeFader.mActive = 0;
                    setVoiceVolume_internal((uint)ch, aVolume);
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Set delay, in samples, before starting to play samples. Calling this on a live sound will cause glitches.
        public void setDelaySamples(Handle aVoiceHandle, uint aSamples)
        {
            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mDelaySamples = aSamples;
                }
            }

            lockAudioMutex_internal();
            ArraySegment<Handle> h_ = voiceGroupHandleToArray_internal(aVoiceHandle);
            if (h_.Array == null)
            {
                body(aVoiceHandle);
            }
            else
            {
                foreach (Handle h in h_.AsSpan())
                    body(h);
            }
            unlockAudioMutex_internal();
        }

        // Enable or disable visualization data gathering
        public void setVisualizationEnable(bool aEnable)
        {
            if (aEnable)
            {
                mFlags |= FLAGS.ENABLE_VISUALIZATION;
            }
            else
            {
                mFlags &= ~FLAGS.ENABLE_VISUALIZATION;
            }
        }

        // Set speaker position in 3d space
        public SOLOUD_ERRORS setSpeakerPosition(uint aChannel, float aX, float aY, float aZ)
        {
            if (aChannel >= mChannels)
                return SOLOUD_ERRORS.INVALID_PARAMETER;

            m3dSpeakerPosition[aChannel] = new Vec3(aX, aY, aZ);
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }
    }
}
