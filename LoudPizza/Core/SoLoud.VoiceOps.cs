using System;
using System.Diagnostics;

namespace LoudPizza
{
    // Direct voice operations (no mutexes - called from other functions)
    public unsafe partial class SoLoud
    {
        // Set voice (not handle) relative play speed.
        internal SOLOUD_ERRORS setVoiceRelativePlaySpeed_internal(uint aVoice, float aSpeed)
        {
            Debug.Assert(aVoice < VOICE_COUNT);
            Debug.Assert(mInsideAudioThreadMutex);

            if (aSpeed <= 0.0f)
            {
                return SOLOUD_ERRORS.INVALID_PARAMETER;
            }

            AudioSourceInstance? voice = mVoice[aVoice];
            if (voice != null)
            {
                voice.mSetRelativePlaySpeed = aSpeed;
                updateVoiceRelativePlaySpeed_internal(aVoice);
            }

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        // Set voice (not handle) pause state.
        internal void setVoicePause_internal(uint aVoice, bool aPause)
        {
            Debug.Assert(aVoice < VOICE_COUNT);
            Debug.Assert(mInsideAudioThreadMutex);

            mActiveVoiceDirty = true;
            AudioSourceInstance? voice = mVoice[aVoice];
            if (voice != null)
            {
                voice.mPauseScheduler.mActive = 0;

                if (aPause)
                {
                    voice.mFlags |= AudioSourceInstance.FLAGS.PAUSED;
                }
                else
                {
                    voice.mFlags &= ~AudioSourceInstance.FLAGS.PAUSED;
                }
            }
        }

        // Set voice (not handle) pan.
        internal void setVoicePan_internal(uint aVoice, float aPan)
        {
            Debug.Assert(aVoice < VOICE_COUNT);
            Debug.Assert(mInsideAudioThreadMutex);

            AudioSourceInstance? voice = mVoice[aVoice];
            if (voice != null)
            {
                voice.mPan = aPan;
                float l = (float)MathF.Cos((aPan + 1) * MathF.PI / 4);
                float r = (float)MathF.Sin((aPan + 1) * MathF.PI / 4);
                voice.mChannelVolume[0] = l;
                voice.mChannelVolume[1] = r;
                if (voice.mChannels == 4)
                {
                    voice.mChannelVolume[2] = l;
                    voice.mChannelVolume[3] = r;
                }
                if (voice.mChannels == 6)
                {
                    voice.mChannelVolume[2] = SQRT2RECP;
                    voice.mChannelVolume[3] = 1;
                    voice.mChannelVolume[4] = l;
                    voice.mChannelVolume[5] = r;
                }
                if (voice.mChannels == 8)
                {
                    voice.mChannelVolume[2] = SQRT2RECP;
                    voice.mChannelVolume[3] = 1;
                    voice.mChannelVolume[4] = l;
                    voice.mChannelVolume[5] = r;
                    voice.mChannelVolume[6] = l;
                    voice.mChannelVolume[7] = r;
                }
            }
        }

        // Set voice (not handle) volume.
        internal void setVoiceVolume_internal(uint aVoice, float aVolume)
        {
            Debug.Assert(aVoice < VOICE_COUNT);
            Debug.Assert(mInsideAudioThreadMutex);

            mActiveVoiceDirty = true;
            AudioSourceInstance? voice = mVoice[aVoice];
            if (voice != null)
            {
                voice.mSetVolume = aVolume;
                updateVoiceVolume_internal(aVoice);
            }
        }

        // Stop voice (not handle).
        internal void stopVoice_internal(uint aVoice)
        {
            Debug.Assert(aVoice < VOICE_COUNT);
            Debug.Assert(mInsideAudioThreadMutex);

            mActiveVoiceDirty = true;
            AudioSourceInstance? voice = mVoice[aVoice];
            if (voice != null)
            {
                // Delete via temporary variable to avoid recursion
                AudioSourceInstance? v = mVoice[aVoice];
                mVoice[aVoice] = null;

                for (uint i = 0; i < mMaxActiveVoices; i++)
                {
                    if (mResampleDataOwner[i] == v)
                    {
                        mResampleDataOwner[i] = null;
                    }
                }

                v?.Dispose();
            }
        }

        // Update overall relative play speed from set and 3d speeds
        internal void updateVoiceRelativePlaySpeed_internal(uint aVoice)
        {
            Debug.Assert(aVoice < VOICE_COUNT);
            Debug.Assert(mInsideAudioThreadMutex);

            AudioSourceInstance? voice = mVoice[aVoice];
            Debug.Assert(voice != null);

            voice.mOverallRelativePlaySpeed = m3dData[aVoice].mDopplerValue * voice.mSetRelativePlaySpeed;
            voice.mSamplerate = voice.mBaseSamplerate * voice.mOverallRelativePlaySpeed;
        }

        // Update overall volume from set and 3d volumes
        internal void updateVoiceVolume_internal(uint aVoice)
        {
            Debug.Assert(aVoice < VOICE_COUNT);
            Debug.Assert(mInsideAudioThreadMutex);

            AudioSourceInstance? voice = mVoice[aVoice];
            Debug.Assert(voice != null);

            voice.mOverallVolume = voice.mSetVolume * m3dData[aVoice].m3dVolume;
            if ((voice.mFlags & AudioSourceInstance.FLAGS.PAUSED) != 0)
            {
                for (int i = 0; i < MAX_CHANNELS; i++)
                {
                    voice.mCurrentChannelVolume[i] = voice.mChannelVolume[i] * voice.mOverallVolume;
                }
            }
        }
    }
}
