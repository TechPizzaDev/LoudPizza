using System;
using System.Runtime.CompilerServices;

namespace LoudPizza
{
    public unsafe partial class SoLoud
    {
        public static float doppler(Vec3 aDeltaPos, Vec3 aSrcVel, Vec3 aDstVel, float aFactor, float aSoundSpeed)
        {
            float deltamag = aDeltaPos.mag();
            if (deltamag == 0)
                return 1.0f;
            float vls = aDeltaPos.dot(aDstVel) / deltamag;
            float vss = aDeltaPos.dot(aSrcVel) / deltamag;
            float maxspeed = aSoundSpeed / aFactor;
            vss = MathF.Min(vss, maxspeed);
            vls = MathF.Min(vls, maxspeed);
            return (aSoundSpeed - aFactor * vls) / (aSoundSpeed - aFactor * vss);
        }

        public static float attenuateInvDistance(float aDistance, float aMinDistance, float aMaxDistance, float aRolloffFactor)
        {
            float distance = MathF.Max(aDistance, aMinDistance);
            distance = MathF.Min(distance, aMaxDistance);
            return aMinDistance / (aMinDistance + aRolloffFactor * (distance - aMinDistance));
        }

        public static float attenuateLinearDistance(float aDistance, float aMinDistance, float aMaxDistance, float aRolloffFactor)
        {
            float distance = MathF.Max(aDistance, aMinDistance);
            distance = MathF.Min(distance, aMaxDistance);
            return 1 - aRolloffFactor * (distance - aMinDistance) / (aMaxDistance - aMinDistance);
        }

        public static float attenuateExponentialDistance(float aDistance, float aMinDistance, float aMaxDistance, float aRolloffFactor)
        {
            float distance = MathF.Max(aDistance, aMinDistance);
            distance = MathF.Min(distance, aMaxDistance);
            return MathF.Pow(distance / aMinDistance, -aRolloffFactor);
        }

        // Perform 3d audio calculation for array of voices
        [SkipLocalsInit]
        internal void update3dVoices_internal(uint* aVoiceArray, uint aVoiceCount)
        {
            Vec3* speaker = stackalloc Vec3[MAX_CHANNELS];

            int i;
            for (i = 0; i < mChannels; i++)
            {
                speaker[i] = m3dSpeakerPosition[i];
                speaker[i].normalize();
            }
            for (; i < MAX_CHANNELS; i++)
            {
                speaker[i] = default;
            }

            Vec3 lpos = m3dPosition;
            Vec3 lvel = m3dVelocity;
            Vec3 at = m3dAt;
            Vec3 up = m3dUp;

            CRuntime.SkipInit(out Mat3 m);
            if ((mFlags & FLAGS.LEFT_HANDED_3D) != 0)
            {
                m.lookatLH(at, up);
            }
            else
            {
                m.lookatRH(at, up);
            }

            for (i = 0; i < aVoiceCount; i++)
            {
                ref AudioSourceInstance3dData v = ref m3dData[aVoiceArray[i]];

                float vol = 1;

                // custom collider
                if (v.mCollider != null)
                {
                    vol *= v.mCollider.collide(this, v, v.mColliderData);
                }

                Vec3 pos = v.m3dPosition;
                Vec3 vel = v.m3dVelocity;

                if ((v.mFlags & AudioSourceInstance.FLAGS.LISTENER_RELATIVE) == 0)
                {
                    pos = pos.sub(lpos);
                }

                float dist = pos.mag();

                // attenuation
                if (v.mAttenuator != null)
                {
                    vol *= v.mAttenuator.attenuate(dist, v.m3dMinDistance, v.m3dMaxDistance, v.m3dAttenuationRolloff);
                }

                // cone

                // (todo) vol *= conev;

                // doppler
                v.mDopplerValue = doppler(pos, vel, lvel, v.m3dDopplerFactor, m3dSoundSpeed);

                // panning
                pos = m.mul(pos);
                pos.normalize();

                // Apply volume to channels based on speaker vectors
                int j;
                for (j = 0; j < mChannels; j++)
                {
                    float finalvol = vol;
                    Vec3 spk = speaker[j];
                    if (!spk.isZero())
                    {
                        float speakervol = (spk.dot(pos) + 1) / 2;

                        // Different speaker "focus" calculations to try, if the default "bleeds" too much..
                        //speakervol = (speakervol * speakervol + speakervol) / 2;
                        //speakervol = speakervol * speakervol;
                        finalvol *= speakervol;
                    }
                    v.mChannelVolume[j] = finalvol;
                }
                for (; j < MAX_CHANNELS; j++)
                {
                    v.mChannelVolume[j] = 0;
                }

                v.m3dVolume = vol;
            }
        }

        // Perform 3d audio parameter update
        [SkipLocalsInit]
        public void update3dAudio()
        {
            uint voicecount = 0;
            uint* voices = stackalloc uint[VOICE_COUNT];

            // Step 1 - find voices that need 3d processing
            lockAudioMutex_internal();
            uint i;
            for (i = 0; i < mHighestVoice; i++)
            {
                AudioSourceInstance? voice = mVoice[i];
                if (voice != null && (voice.mFlags & AudioSourceInstance.FLAGS.PROCESS_3D) != 0)
                {
                    voices[voicecount] = i;
                    voicecount++;
                    m3dData[i].mFlags = voice.mFlags;
                }
            }
            unlockAudioMutex_internal();

            // Step 2 - do 3d processing

            update3dVoices_internal(voices, voicecount);

            // Step 3 - update SoLoud voices

            lockAudioMutex_internal();
            for (i = 0; i < voicecount; i++)
            {
                AudioSourceInstance? vi = mVoice[voices[i]];
                if (vi != null)
                {
                    ref AudioSourceInstance3dData v = ref m3dData[voices[i]];
                    updateVoiceRelativePlaySpeed_internal(voices[i]);
                    updateVoiceVolume_internal(voices[i]);
                    vi.mChannelVolume = v.mChannelVolume;

                    if (vi.mOverallVolume < 0.001f)
                    {
                        // Inaudible.
                        vi.mFlags |= AudioSourceInstance.FLAGS.INAUDIBLE;

                        if ((vi.mFlags & AudioSourceInstance.FLAGS.INAUDIBLE_KILL) != 0)
                        {
                            stopVoice_internal(voices[i]);
                        }
                    }
                    else
                    {
                        vi.mFlags &= ~AudioSourceInstance.FLAGS.INAUDIBLE;
                    }
                }
            }

            mActiveVoiceDirty = true;
            unlockAudioMutex_internal();
        }

        // Start playing a 3d audio source
        public Handle play3d(AudioSource aSound, float aPosX, float aPosY, float aPosZ, float aVelX = 0.0f, float aVelY = 0.0f, float aVelZ = 0.0f, float aVolume = 1.0f, bool aPaused = false, Handle aBus = default)
        {
            Handle h = play(aSound, aVolume, 0, true, aBus);
            lockAudioMutex_internal();
            int v = getVoiceFromHandle_internal(h);
            if (v < 0)
            {
                unlockAudioMutex_internal();
                return h;
            }
            m3dData[v].mHandle = h;
            AudioSourceInstance voice = mVoice[v]!;
            voice.mFlags |= AudioSourceInstance.FLAGS.PROCESS_3D;
            set3dSourceParameters(h, aPosX, aPosY, aPosZ, aVelX, aVelY, aVelZ);

            int samples = 0;
            if ((aSound.mFlags & AudioSource.FLAGS.DISTANCE_DELAY) != 0)
            {
                Vec3 pos;
                pos.X = aPosX;
                pos.Y = aPosY;
                pos.Z = aPosZ;
                if (((uint)voice.mFlags & (uint)AudioSource.FLAGS.LISTENER_RELATIVE) == 0)
                {
                    pos = pos.sub(m3dPosition);
                }
                float dist = pos.mag();
                samples += (int)MathF.Floor((dist / m3dSoundSpeed) * mSamplerate);
            }

            update3dVoices_internal((uint*)&v, 1);
            updateVoiceRelativePlaySpeed_internal((uint)v);
            voice.mChannelVolume = m3dData[v].mChannelVolume;

            updateVoiceVolume_internal((uint)v);

            // Fix initial voice volume ramp up
            for (int i = 0; i < MAX_CHANNELS; i++)
            {
                voice.mCurrentChannelVolume[i] = voice.mChannelVolume[i] * voice.mOverallVolume;
            }

            if (voice.mOverallVolume < 0.01f)
            {
                // Inaudible.
                voice.mFlags |= AudioSourceInstance.FLAGS.INAUDIBLE;

                if ((voice.mFlags & AudioSourceInstance.FLAGS.INAUDIBLE_KILL) != 0)
                {
                    stopVoice_internal((uint)v);
                }
            }
            else
            {
                voice.mFlags &= ~AudioSourceInstance.FLAGS.INAUDIBLE;
            }
            mActiveVoiceDirty = true;

            unlockAudioMutex_internal();
            setDelaySamples(h, (uint)samples);
            setPause(h, aPaused);
            return h;
        }

        // Start playing a 3d audio source, delayed in relation to other sounds called via this function.
        public Handle play3dClocked(Time aSoundTime, AudioSource aSound, float aPosX, float aPosY, float aPosZ, float aVelX = 0.0f, float aVelY = 0.0f, float aVelZ = 0.0f, float aVolume = 1.0f, Handle aBus = default)
        {
            Handle h = play(aSound, aVolume, 0, true, aBus);
            lockAudioMutex_internal();
            int v = getVoiceFromHandle_internal(h);
            if (v < 0)
            {
                unlockAudioMutex_internal();
                return h;
            }
            m3dData[v].mHandle = h;
            AudioSourceInstance voice = mVoice[v]!;
            voice.mFlags |= AudioSourceInstance.FLAGS.PROCESS_3D;
            set3dSourceParameters(h, aPosX, aPosY, aPosZ, aVelX, aVelY, aVelZ);
            Time lasttime = mLastClockedTime;
            if (lasttime == 0)
            {
                lasttime = aSoundTime;
                mLastClockedTime = aSoundTime;
            }
            Vec3 pos;
            pos.X = aPosX;
            pos.Y = aPosY;
            pos.Z = aPosZ;
            unlockAudioMutex_internal();

            int samples = (int)Math.Floor((aSoundTime - lasttime) * mSamplerate);
            // Make sure we don't delay too much (or overflow)
            if (samples < 0 || samples > 2048)
                samples = 0;

            if ((aSound.mFlags & AudioSource.FLAGS.DISTANCE_DELAY) != 0)
            {
                float dist = pos.mag();
                samples += (int)MathF.Floor((dist / m3dSoundSpeed) * mSamplerate);
            }

            update3dVoices_internal((uint*)&v, 1);
            lockAudioMutex_internal();
            updateVoiceRelativePlaySpeed_internal((uint)v);
            voice.mChannelVolume = m3dData[v].mChannelVolume;

            updateVoiceVolume_internal((uint)v);

            // Fix initial voice volume ramp up
            for (int i = 0; i < MAX_CHANNELS; i++)
            {
                voice.mCurrentChannelVolume[i] = voice.mChannelVolume[i] * voice.mOverallVolume;
            }

            if (voice.mOverallVolume < 0.01f)
            {
                // Inaudible.
                voice.mFlags |= AudioSourceInstance.FLAGS.INAUDIBLE;

                if ((voice.mFlags & AudioSourceInstance.FLAGS.INAUDIBLE_KILL) != 0)
                {
                    stopVoice_internal((uint)v);
                }
            }
            else
            {
                voice.mFlags &= ~AudioSourceInstance.FLAGS.INAUDIBLE;
            }
            mActiveVoiceDirty = true;
            unlockAudioMutex_internal();

            setDelaySamples(h, (uint)samples);
            setPause(h, false);
            return h;
        }

        // Set the speed of sound constant for doppler
        public SOLOUD_ERRORS set3dSoundSpeed(float aSpeed)
        {
            if (aSpeed <= 0)
                return SOLOUD_ERRORS.INVALID_PARAMETER;
            m3dSoundSpeed = aSpeed;
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        // Get the current speed of sound constant for doppler
        public float get3dSoundSpeed()
        {
            return m3dSoundSpeed;
        }

        // Set 3d listener parameters
        public void set3dListenerParameters(float aPosX, float aPosY, float aPosZ, float aAtX, float aAtY, float aAtZ, float aUpX, float aUpY, float aUpZ, float aVelocityX, float aVelocityY, float aVelocityZ)
        {
            m3dPosition = new Vec3(aPosX, aPosY, aPosZ);
            m3dAt = new Vec3(aAtX, aAtY, aAtZ);
            m3dUp = new Vec3(aUpX, aUpY, aUpZ);
            m3dVelocity = new Vec3(aVelocityX, aVelocityY, aVelocityZ);
        }

        // Set 3d listener position
        public void set3dListenerPosition(float aPosX, float aPosY, float aPosZ)
        {
            m3dPosition = new Vec3(aPosX, aPosY, aPosZ);
        }

        // Set 3d listener "at" vector
        public void set3dListenerAt(float aAtX, float aAtY, float aAtZ)
        {
            m3dAt = new Vec3(aAtX, aAtY, aAtZ);
        }

        // set 3d listener "up" vector
        public void set3dListenerUp(float aUpX, float aUpY, float aUpZ)
        {
            m3dUp = new Vec3(aUpX, aUpY, aUpZ);
        }

        // Set 3d listener velocity
        public void set3dListenerVelocity(float aVelocityX, float aVelocityY, float aVelocityZ)
        {
            m3dVelocity = new Vec3(aVelocityX, aVelocityY, aVelocityZ);
        }

        // Set 3d audio source parameters
        public void set3dSourceParameters(Handle aVoiceHandle, float aPosX, float aPosY, float aPosZ, float aVelocityX, float aVelocityY, float aVelocityZ)
        {
            void body(Handle h)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dPosition = new Vec3(aPosX, aPosY, aPosZ);
                    m3dData[ch].m3dVelocity = new Vec3(aVelocityX, aVelocityY, aVelocityZ);
                }
            }

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
        }

        // Set 3d audio source position
        public void set3dSourcePosition(Handle aVoiceHandle, float aPosX, float aPosY, float aPosZ)
        {
            void body(Handle h)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dPosition = new Vec3(aPosX, aPosY, aPosZ);
                }
            }

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
        }

        // Set 3d audio source velocity
        public void set3dSourceVelocity(Handle aVoiceHandle, float aVelocityX, float aVelocityY, float aVelocityZ)
        {
            void body(Handle h)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dVelocity = new Vec3(aVelocityX, aVelocityY, aVelocityZ);
                }
            }

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
        }

        // Set 3d audio source min/max distance (distance < min means max volume)
        public void set3dSourceMinMaxDistance(Handle aVoiceHandle, float aMinDistance, float aMaxDistance)
        {
            void body(Handle h)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dMinDistance = aMinDistance;
                    m3dData[ch].m3dMaxDistance = aMaxDistance;
                }
            }

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
        }

        // Set 3d audio source collider. Set to NULL to disable.
        public void set3dSourceCollider(Handle aVoiceHandle, AudioCollider? aCollider, int aUserData = 0)
        {
            void body(Handle h)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].mCollider = aCollider;
                    m3dData[ch].mColliderData = aUserData;
                }
            }

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
        }

        // Set 3d audio source attenuation rolloff factor
        public void set3dSourceAttenuationRolloffFactor(Handle aVoiceHandle, float aAttenuationRolloffFactor)
        {
            void body(Handle h)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dAttenuationRolloff = aAttenuationRolloffFactor;
                }
            }

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
        }

        // Set 3d audio source attenuator. Set to NULL to disable.
        public void set3dSourceAttenuator(Handle aVoiceHandle, AudioAttenuator? aAttenuator)
        {
            void body(Handle h)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].mAttenuator = aAttenuator;
                }
            }

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
        }

        // Set 3d audio source doppler factor to reduce or enhance doppler effect. Default = 1.0
        public void set3dSourceDopplerFactor(Handle aVoiceHandle, float aDopplerFactor)
        {
            void body(Handle h)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dDopplerFactor = aDopplerFactor;
                }
            }

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
        }
    }
}
