using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using LoudPizza.Sources;

namespace LoudPizza.Core
{
    public unsafe partial class SoLoud
    {
        public static float doppler(Vector3 aDeltaPos, Vector3 aSrcVel, Vector3 aDstVel, float aFactor, float aSoundSpeed)
        {
            float deltamag = aDeltaPos.Length();
            if (deltamag == 0)
                return 1.0f;
            float vls = Vector3.Dot(aDeltaPos, aDstVel) / deltamag;
            float vss = Vector3.Dot(aDeltaPos, aSrcVel) / deltamag;
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

        /// <summary>
        /// Perform 3D audio calculation for array of voices.
        /// </summary>
        [SkipLocalsInit]
        internal void update3dVoices_internal(uint* aVoiceArray, uint aVoiceCount)
        {
            Vector3* speaker = stackalloc Vector3[MaxChannels];

            int i;
            for (i = 0; i < mChannels; i++)
            {
                speaker[i] = Vector3Extensions.SafeNormalize(m3dSpeakerPosition[i]);
            }
            for (; i < MaxChannels; i++)
            {
                speaker[i] = default;
            }

            Vector3 lpos = m3dPosition;
            Vector3 lvel = m3dVelocity;
            Vector3 at = m3dAt;
            Vector3 up = m3dUp;

            Unsafe.SkipInit(out Mat3 m);
            if ((mFlags & Flags.LeftHanded3D) != 0)
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
                    vol *= v.mCollider.Collide(this, v);
                }

                Vector3 pos = v.m3dPosition;
                Vector3 vel = v.m3dVelocity;

                if ((v.mFlags & AudioSourceInstance.Flags.ListenerRelative) == 0)
                {
                    pos -= lpos;
                }

                // attenuation
                if (v.mAttenuator != null)
                {
                    float dist = pos.Length();
                    vol *= v.mAttenuator.Attenuate(dist, v.m3dMinDistance, v.m3dMaxDistance, v.m3dAttenuationRolloff);
                }

                // cone

                // (todo) vol *= conev;

                // doppler
                v.mDopplerValue = doppler(pos, vel, lvel, v.m3dDopplerFactor, m3dSoundSpeed);

                // panning
                pos = Vector3Extensions.SafeNormalize(m.mul(pos));

                // Apply volume to channels based on speaker vectors
                int j;
                for (j = 0; j < mChannels; j++)
                {
                    float finalvol = vol;
                    Vector3 spk = speaker[j];
                    if (spk.LengthSquared() != 0)
                    {
                        float speakervol = (Vector3.Dot(spk, pos) + 1) / 2;

                        // Different speaker "focus" calculations to try, if the default "bleeds" too much..
                        //speakervol = (speakervol * speakervol + speakervol) / 2;
                        //speakervol = speakervol * speakervol;
                        finalvol *= speakervol;
                    }
                    v.mChannelVolume[j] = finalvol;
                }
                for (; j < MaxChannels; j++)
                {
                    v.mChannelVolume[j] = 0;
                }

                v.m3dVolume = vol;
            }
        }

        /// <summary>
        /// Perform 3D audio parameter update.
        /// </summary>
        /// <remarks>
        /// Has to be called periodically to internally synchronize 3D audio parameters.
        /// </remarks>
        [SkipLocalsInit]
        public void update3dAudio()
        {
            uint voicecount = 0;
            uint* voices = stackalloc uint[MaxVoiceCount];

            // Step 1 - find voices that need 3D processing
            lock (mAudioThreadMutex)
            {
                for (uint i = 0; i < mHighestVoice; i++)
                {
                    AudioSourceInstance? voice = mVoice[i];
                    if (voice != null && (voice.mFlags & AudioSourceInstance.Flags.Process3D) != 0)
                    {
                        voices[voicecount] = i;
                        voicecount++;
                        m3dData[i].mFlags = voice.mFlags;
                    }
                }
            }

            // Step 2 - do 3D processing

            update3dVoices_internal(voices, voicecount);

            // Step 3 - update SoLoud voices

            lock (mAudioThreadMutex)
            {
                for (uint i = 0; i < voicecount; i++)
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
                            vi.mFlags |= AudioSourceInstance.Flags.Inaudible;

                            if ((vi.mFlags & AudioSourceInstance.Flags.InaudibleKill) != 0)
                            {
                                stopVoice_internal(voices[i]);
                            }
                        }
                        else
                        {
                            vi.mFlags &= ~AudioSourceInstance.Flags.Inaudible;
                        }
                    }
                }

                mActiveVoiceDirty = true;
            }
        }

        /// <summary>
        /// Start playing a 3D audio source.
        /// </summary>
        public Handle play3d(
            AudioSource aSound,
            Vector3 aPosition,
            Vector3 aVelocity = default,
            float aVolume = 1.0f,
            bool aPaused = false,
            Handle aBus = default)
        {
            int v;
            AudioSourceInstance voice;
            int samples = 0;

            Handle h = play(aSound, aVolume, 0, true, aBus);
            lock (mAudioThreadMutex)
            {
                v = getVoiceFromHandle_internal(h);
                if (v < 0)
                {
                    return h;
                }
                m3dData[v].mHandle = h;
                voice = mVoice[v]!;
                voice.mFlags |= AudioSourceInstance.Flags.Process3D;
                set3dSourceParameters(h, aPosition, aVelocity);

                if ((aSound.mFlags & AudioSource.Flags.DistanceDelay) != 0)
                {
                    Vector3 pos = aPosition;
                    if (((uint)voice.mFlags & (uint)AudioSource.Flags.ListenerRelative) == 0)
                    {
                        pos -= m3dPosition;
                    }
                    float dist = pos.LengthSquared();
                    samples += (int)MathF.Floor((dist / (m3dSoundSpeed * m3dSoundSpeed)) * mSamplerate);
                }
            }

            update3dVoices_internal((uint*)&v, 1);

            lock (mAudioThreadMutex)
            {
                updateVoiceRelativePlaySpeed_internal((uint)v);
                voice.mChannelVolume = m3dData[v].mChannelVolume;

                updateVoiceVolume_internal((uint)v);

                // Fix initial voice volume ramp up
                for (int i = 0; i < MaxChannels; i++)
                {
                    voice.mCurrentChannelVolume[i] = voice.mChannelVolume[i] * voice.mOverallVolume;
                }

                if (voice.mOverallVolume < 0.01f)
                {
                    // Inaudible.
                    voice.mFlags |= AudioSourceInstance.Flags.Inaudible;

                    if ((voice.mFlags & AudioSourceInstance.Flags.InaudibleKill) != 0)
                    {
                        stopVoice_internal((uint)v);
                    }
                }
                else
                {
                    voice.mFlags &= ~AudioSourceInstance.Flags.Inaudible;
                }
                mActiveVoiceDirty = true;
            }

            setDelaySamples(h, (uint)samples);
            setPause(h, aPaused);
            return h;
        }

        VoiceHandle IAudioBus.Play3D(
            AudioSource sound,
            Vector3 position,
            Vector3 velocity,
            float volume,
            bool paused)
        {
            Handle handle = play3d(sound, position, velocity, volume, paused, default);
            return new VoiceHandle(this, handle);
        }

        /// <summary>
        /// Start playing a 3D audio source, delayed in relation to other sounds called via this function.
        /// </summary>
        public Handle play3dClocked(
            Time aSoundTime,
            AudioSource aSound,
            Vector3 aPosition,
            Vector3 aVelocity = default,
            float aVolume = 1.0f,
            Handle aBus = default)
        {
            int v;
            AudioSourceInstance voice;
            int samples;

            Handle h = play(aSound, aVolume, 0, true, aBus);
            lock (mAudioThreadMutex)
            {
                v = getVoiceFromHandle_internal(h);
                if (v < 0)
                {
                    return h;
                }
                m3dData[v].mHandle = h;
                voice = mVoice[v]!;
                voice.mFlags |= AudioSourceInstance.Flags.Process3D;
                set3dSourceParameters(h, aPosition, aVelocity);
                Time lasttime = mLastClockedTime;
                if (lasttime == 0)
                {
                    lasttime = aSoundTime;
                    mLastClockedTime = aSoundTime;
                }

                samples = (int)Math.Floor((aSoundTime - lasttime) * mSamplerate);
                // Make sure we don't delay too much (or overflow)
                if (samples < 0 || samples > 2048)
                    samples = 0;

                if ((aSound.mFlags & AudioSource.Flags.DistanceDelay) != 0)
                {
                    float dist = aPosition.LengthSquared();
                    samples += (int)MathF.Floor((dist / (m3dSoundSpeed * m3dSoundSpeed)) * mSamplerate);
                }
            }

            update3dVoices_internal((uint*)&v, 1);

            lock (mAudioThreadMutex)
            {
                updateVoiceRelativePlaySpeed_internal((uint)v);
                voice.mChannelVolume = m3dData[v].mChannelVolume;

                updateVoiceVolume_internal((uint)v);

                // Fix initial voice volume ramp up
                for (int i = 0; i < MaxChannels; i++)
                {
                    voice.mCurrentChannelVolume[i] = voice.mChannelVolume[i] * voice.mOverallVolume;
                }

                if (voice.mOverallVolume < 0.01f)
                {
                    // Inaudible.
                    voice.mFlags |= AudioSourceInstance.Flags.Inaudible;

                    if ((voice.mFlags & AudioSourceInstance.Flags.InaudibleKill) != 0)
                    {
                        stopVoice_internal((uint)v);
                    }
                }
                else
                {
                    voice.mFlags &= ~AudioSourceInstance.Flags.Inaudible;
                }
                mActiveVoiceDirty = true;
            }

            setDelaySamples(h, (uint)samples);
            setPause(h, false);
            return h;
        }

        VoiceHandle IAudioBus.PlayClocked3D(
            AudioSource source,
            Time soundTime,
            Vector3 position,
            Vector3 velocity,
            float volume)
        {
            Handle handle = play3dClocked(soundTime, source, position, velocity, volume, default);
            return new VoiceHandle(this, handle);
        }

        /// <summary>
        /// Set the speed of sound constant for doppler.
        /// </summary>
        public SoLoudStatus set3dSoundSpeed(float aSpeed)
        {
            if (aSpeed <= 0)
                return SoLoudStatus.InvalidParameter;
            m3dSoundSpeed = aSpeed;
            return SoLoudStatus.Ok;
        }

        /// <summary>
        /// Get the current speed of sound constant for doppler.
        /// </summary>
        public float get3dSoundSpeed()
        {
            return m3dSoundSpeed;
        }

        /// <summary>
        /// Set 3D listener parameters.
        /// </summary>
        public void set3dListenerParameters(
            Vector3 aPosition,
            Vector3 aAt,
            Vector3 aUp,
            Vector3 aVelocity)
        {
            m3dPosition = aPosition;
            m3dAt = aAt;
            m3dUp = aUp;
            m3dVelocity = aVelocity;
        }

        /// <summary>
        /// Set 3D listener position.
        /// </summary>
        public void set3dListenerPosition(Vector3 aPosition)
        {
            m3dPosition = aPosition;
        }

        /// <summary>
        /// Set 3D listener "at" vector.
        /// </summary>
        public void set3dListenerAt(Vector3 aAt)
        {
            m3dAt = aAt;
        }

        /// <summary>
        /// Set 3D listener "up" vector.
        /// </summary>
        public void set3dListenerUp(Vector3 aUp)
        {
            m3dUp = aUp;
        }

        /// <summary>
        /// Set 3D listener velocity.
        /// </summary>
        public void set3dListenerVelocity(Vector3 aVelocity)
        {
            m3dVelocity = aVelocity;
        }

        /// <summary>
        /// Set 3D audio source parameters.
        /// </summary>
        public void set3dSourceParameters(
            Handle aVoiceHandle,
            Vector3 aPosition,
            Vector3 aVelocity)
        {
            ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
            foreach (Handle h in h_)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dPosition = aPosition;
                    m3dData[ch].m3dVelocity = aVelocity;
                }
            }
        }

        /// <summary>
        /// Set 3D audio source position.
        /// </summary>
        public void set3dSourcePosition(Handle aVoiceHandle, Vector3 aPosition)
        {
            ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
            foreach (Handle h in h_)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dPosition = aPosition;
                }
            }
        }

        /// <summary>
        /// Set 3D audio source velocity.
        /// </summary>
        public void set3dSourceVelocity(Handle aVoiceHandle, Vector3 aVelocity)
        {
            ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
            foreach (Handle h in h_)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dVelocity = aVelocity;
                }
            }
        }

        /// <summary>
        /// Set 3D audio source min/max distance (distance less than min means max volume).
        /// </summary>
        public void set3dSourceMinMaxDistance(Handle aVoiceHandle, float aMinDistance, float aMaxDistance)
        {
            ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
            foreach (Handle h in h_)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dMinDistance = aMinDistance;
                    m3dData[ch].m3dMaxDistance = aMaxDistance;
                }
            }
        }

        /// <summary>
        /// Set 3D audio source collider and data. Set to <see langword="null"/> to disable.
        /// </summary>
        public void set3dSourceCollider(Handle aVoiceHandle, AudioCollider? aCollider, IntPtr aUserData = default)
        {
            ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
            foreach (Handle h in h_)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].mCollider = aCollider;
                    m3dData[ch].mColliderData = aUserData;
                }
            }
        }

        /// <summary>
        /// Set 3D audio source attenuation rolloff factor.
        /// </summary>
        public void set3dSourceAttenuationRolloffFactor(Handle aVoiceHandle, float aAttenuationRolloffFactor)
        {
            ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
            foreach (Handle h in h_)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dAttenuationRolloff = aAttenuationRolloffFactor;
                }
            }
        }

        /// <summary>
        /// Set 3D audio source attenuator. Set to <see langword="null"/> to disable.
        /// </summary>
        public void set3dSourceAttenuator(Handle aVoiceHandle, AudioAttenuator? aAttenuator)
        {
            ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
            foreach (Handle h in h_)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].mAttenuator = aAttenuator;
                }
            }
        }

        /// <summary>
        /// Set 3D audio source doppler factor to reduce or enhance doppler effect (default = 1.0).
        /// </summary>
        public void set3dSourceDopplerFactor(Handle aVoiceHandle, float aDopplerFactor)
        {
            ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
            foreach (Handle h in h_)
            {
                int ch = (int)(h.Value & 0xfff - 1);
                if (ch != -1 && m3dData[ch].mHandle == h)
                {
                    m3dData[ch].m3dDopplerFactor = aDopplerFactor;
                }
            }
        }
    }
}
