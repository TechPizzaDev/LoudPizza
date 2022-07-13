using System;

namespace LoudPizza
{
    // Setters - set various bits of SoLoud state
    public unsafe partial class SoLoud
    {
        /// <summary>
        /// Set the post clip scaler value.
        /// </summary>
        public void setPostClipScaler(float aScaler)
        {
            mPostClipScaler = aScaler;
        }

        /// <summary>
        /// Set the main resampler.
        /// </summary>
        public void setMainResampler(RESAMPLER aResampler)
        {
            if (aResampler <= RESAMPLER.RESAMPLER_CATMULLROM)
                mResampler = aResampler;
        }

        /// <summary>
        /// Set the global volume.
        /// </summary>
        public void setGlobalVolume(float aVolume)
        {
            mGlobalVolumeFader.mActive = 0;
            mGlobalVolume = aVolume;
        }

        /// <summary>
        /// Set the relative play speed.
        /// </summary>
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

        /// <summary>
        /// Set the sample rate.
        /// </summary>
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

        /// <summary>
        /// Set the pause state.
        /// </summary>
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

        /// <summary>
        /// Set current maximum active voice setting.
        /// </summary>
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

        /// <summary>
        /// Pause all voices.
        /// </summary>
        public void setPauseAll(bool aPause)
        {
            lockAudioMutex_internal();
            for (uint ch = 0; ch < mHighestVoice; ch++)
            {
                setVoicePause_internal(ch, aPause);
            }
            unlockAudioMutex_internal();
        }

        /// <summary>
        /// Set the voice protection state.
        /// </summary>
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

        /// <summary>
        /// Set panning value; -1 is left, 0 is center, 1 is right.
        /// </summary>
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

        /// <summary>
        /// Set channel volume (volume for a specific speaker).
        /// </summary>
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

        /// <summary>
        /// Set absolute left/right volumes.
        /// </summary>
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

        /// <summary>
        /// Set behavior for inaudible sounds.
        /// </summary>
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

        /// <summary>
        /// Set voice loop point value.
        /// </summary>
        public void setLoopPoint(Handle aVoiceHandle, ulong aLoopPoint)
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

        /// <summary>
        /// Set voice's loop state.
        /// </summary>
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

        /// <summary>
        /// Set whether sound should auto-stop when it ends.
        /// </summary>
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

        /// <summary>
        /// Set overall volume.
        /// </summary>
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

        /// <summary>
        /// Set delay, in samples, before starting to play samples.
        /// </summary>
        /// <remarks>
        /// Calling this on a live sound will cause glitches.
        /// </remarks>
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

        /// <summary>
        /// Enable or disable visualization data gathering.
        /// </summary>
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

        /// <summary>
        /// Set speaker position in 3D space.
        /// </summary>
        public SOLOUD_ERRORS setSpeakerPosition(uint aChannel, float aX, float aY, float aZ)
        {
            if (aChannel >= mChannels)
                return SOLOUD_ERRORS.INVALID_PARAMETER;

            m3dSpeakerPosition[aChannel] = new Vec3(aX, aY, aZ);
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }
    }
}
