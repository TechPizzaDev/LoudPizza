using System;
using LoudPizza.Modifiers;
using LoudPizza.Sources;

namespace LoudPizza.Core
{
    public unsafe partial class SoLoud : IAudioBus
    {
        /// <summary>
        /// Start playing a sound. 
        /// Returns voice handle, which can be ignored or used to alter the playing sound's parameters. 
        /// Negative volume means to use default.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The source was not constructed from this library instance.
        /// </exception>
        public Handle play(AudioSource aSound, float aVolume = -1.0f, float aPan = 0.0f, bool aPaused = false, Handle aBus = default)
        {
            if (aSound.SoLoud != this)
            {
                throw new InvalidOperationException("The source was not constructed from this library instance.");
            }

            if ((aSound.mFlags & AudioSource.Flags.SingleInstance) != 0)
            {
                // Only one instance allowed, stop others
                aSound.Stop();
            }

            // Creation of an audio instance may take significant amount of time,
            // so let's not do it inside the audio thread mutex.
            AudioSourceInstance instance = aSound.CreateInstance();

            lock (mAudioThreadMutex)
            {
                int ch = findFreeVoice_internal();
                if (ch < 0)
                {
                    instance.Dispose();
                    return new Handle((uint)SoLoudStatus.UnknownError);
                }
                mVoice[ch] = instance;
                instance.mBusHandle = aBus;
                instance.Initialize(mPlayIndex);
                m3dData[ch].init(aSound);

                mPlayIndex++;

                // 20 bits, skip the last one (top bits full = voice group)
                if (mPlayIndex == 0xfffff)
                {
                    mPlayIndex = 0;
                }

                if (aPaused)
                {
                    instance.mFlags |= AudioSourceInstance.Flags.Paused;
                }

                setVoicePan_internal((uint)ch, aPan);
                if (aVolume < 0)
                {
                    setVoiceVolume_internal((uint)ch, aSound.mVolume);
                }
                else
                {
                    setVoiceVolume_internal((uint)ch, aVolume);
                }

                // Fix initial voice volume ramp up		
                int i;
                for (i = 0; i < MaxChannels; i++)
                {
                    instance.mCurrentChannelVolume[i] = instance.mChannelVolume[i] * instance.mOverallVolume;
                }

                setVoiceRelativePlaySpeed_internal((uint)ch, 1);

                for (i = 0; i < FiltersPerStream; i++)
                {
                    Filter? filter = aSound.mFilter[i];
                    if (filter != null)
                    {
                        instance.mFilter[i] = filter.CreateInstance();
                    }
                }

                mActiveVoiceDirty = true;

                Handle handle = getHandleFromVoice_internal((uint)ch);
                return handle;
            }
        }

        VoiceHandle IAudioBus.Play(AudioSource source, float volume, float pan, bool paused)
        {
            Handle handle = play(source, volume, pan, paused, default);
            return new VoiceHandle(this, handle);
        }

        /// <summary>
        /// Start playing a sound delayed in relation to other sounds called via this function. 
        /// Negative volume means to use default.
        /// </summary>
        public Handle playClocked(Time aSoundTime, AudioSource aSound, float aVolume = -1.0f, float aPan = 0.0f, Handle aBus = default)
        {
            Handle h = play(aSound, aVolume, aPan, true, aBus);
            Time lasttime;
            lock (mAudioThreadMutex)
            {
                // mLastClockedTime is cleared to zero at start of every output buffer
                lasttime = mLastClockedTime;
                if (lasttime == 0)
                {
                    mLastClockedTime = aSoundTime;
                    lasttime = aSoundTime;
                }
            }
            int samples = (int)Math.Floor((aSoundTime - lasttime) * mSamplerate);
            // Make sure we don't delay too much (or overflow)
            if (samples < 0 || samples > 2048)
                samples = 0;
            setDelaySamples(h, (uint)samples);
            setPause(h, false);
            return h;
        }

        VoiceHandle IAudioBus.PlayClocked(AudioSource source, Time soundTime, float volume, float pan)
        {
            Handle handle = playClocked(soundTime, source, volume, default);
            return new VoiceHandle(this, handle);
        }

        /// <summary>
        /// Start playing a sound without any panning.
        /// </summary>
        /// <remarks>
        /// It will be played at full volume.
        /// </remarks>
        public Handle playBackground(AudioSource aSound, float aVolume = 1.0f, bool aPaused = false, Handle aBus = default)
        {
            Handle h = play(aSound, aVolume, 0.0f, aPaused, aBus);
            setPanAbsolute(h, 1.0f, 1.0f);
            return h;
        }

        VoiceHandle IAudioBus.PlayBackground(AudioSource source, float volume, bool paused)
        {
            Handle handle = playBackground(source, volume, default);
            return new VoiceHandle(this, handle);
        }

        /// <summary>
        /// Seek the audio stream to certain point in time.
        /// </summary>
        /// <remarks>
        /// Some streams can't seek backwards. 
        /// </remarks>
        public SoLoudStatus seek(Handle aVoiceHandle, ulong aSamplePosition)
        {
            lock (mAudioThreadMutex)
            {
                SoLoudStatus res = SoLoudStatus.Ok;

                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        SoLoudStatus singleres = ch.Seek(aSamplePosition, mScratch.AsSpan());
                        if (singleres != SoLoudStatus.Ok)
                            res = singleres;
                    }
                }
                return res;
            }
        }

        /// <summary>
        /// Stop the voice.
        /// </summary>
        public void stop(Handle aVoiceHandle)
        {
            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    int ch = getVoiceFromHandle_internal(h);
                    if (ch != -1)
                    {
                        stopVoice_internal((uint)ch);
                    }
                }
            }
        }

        /// <summary>
        /// Stop all voices that are playing from the given audio source.
        /// </summary>
        public void stopAudioSource(AudioSource aSound)
        {
            lock (mAudioThreadMutex)
            {
                for (uint i = 0; i < mHighestVoice; i++)
                {
                    AudioSourceInstance? voice = mVoice[i];
                    if (voice != null && voice.Source == aSound)
                    {
                        stopVoice_internal(i);
                    }
                }
            }
        }

        /// <summary>
        /// Stop all voices.
        /// </summary>
        public void stopAll()
        {
            lock (mAudioThreadMutex)
            {
                for (uint i = 0; i < mHighestVoice; i++)
                {
                    stopVoice_internal(i);
                }
            }
        }

        /// <summary>
        /// Gets the amount of voices that play the given audio source.
        /// </summary>
        public int countAudioSource(AudioSource aSound)
        {
            int count = 0;
            lock (mAudioThreadMutex)
            {
                for (uint i = 0; i < mHighestVoice; i++)
                {
                    AudioSourceInstance? voice = mVoice[i];
                    if (voice != null && voice.Source == aSound)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Move a live sound to the given bus.
        /// </summary>
        public void AnnexSound(Handle voiceHandle, Handle busHandle)
        {
            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref voiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        ch.mBusHandle = busHandle;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void AnnexSound(Handle voiceHandle)
        {
            AnnexSound(voiceHandle, default);
        }

        /// <inheritdoc/>
        public Handle GetBusHandle()
        {
            return default;
        }
    }
}
