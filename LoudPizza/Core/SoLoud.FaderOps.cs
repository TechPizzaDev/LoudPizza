using System;

namespace LoudPizza
{
    public unsafe partial class SoLoud
    {
        /// <summary>
        /// Schedule a stream to pause.
        /// </summary>
        public void schedulePause(Handle aVoiceHandle, Time aTime)
        {
            if (aTime <= 0)
            {
                setPause(aVoiceHandle, true);
                return;
            }

            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        ch.mPauseScheduler.set(1, 0, aTime, ch.mStreamTime);
                    }
                }
            }
        }

        /// <summary>
        /// Schedule a stream to stop.
        /// </summary>
        public void scheduleStop(Handle aVoiceHandle, Time aTime)
        {
            if (aTime <= 0)
            {
                stop(aVoiceHandle);
                return;
            }

            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        ch.mStopScheduler.set(1, 0, aTime, ch.mStreamTime);
                    }
                }
            }
        }

        /// <summary>
        /// Set up volume fader.
        /// </summary>
        public void fadeVolume(Handle aVoiceHandle, float aTo, Time aTime)
        {
            float from = getVolume(aVoiceHandle);
            if (aTime <= 0 || aTo == from)
            {
                setVolume(aVoiceHandle, aTo);
                return;
            }

            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        ch.mVolumeFader.set(from, aTo, aTime, ch.mStreamTime);
                    }
                }
            }
        }

        /// <summary>
        /// Set up panning fader.
        /// </summary>
        public void fadePan(Handle aVoiceHandle, float aTo, Time aTime)
        {
            float from = getPan(aVoiceHandle);
            if (aTime <= 0 || aTo == from)
            {
                setPan(aVoiceHandle, aTo);
                return;
            }

            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        ch.mPanFader.set(from, aTo, aTime, ch.mStreamTime);
                    }
                }
            }
        }

        /// <summary>
        /// Set up relative play speed fader.
        /// </summary>
        public void fadeRelativePlaySpeed(Handle aVoiceHandle, float aTo, Time aTime)
        {
            float from = getRelativePlaySpeed(aVoiceHandle);
            if (aTime <= 0 || aTo == from)
            {
                setRelativePlaySpeed(aVoiceHandle, aTo);
                return;
            }

            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        ch.mRelativePlaySpeedFader.set(from, aTo, aTime, ch.mStreamTime);
                    }
                }
            }
        }

        /// <summary>
        /// Set up global volume fader.
        /// </summary>
        public void fadeGlobalVolume(float aTo, Time aTime)
        {
            float from = getGlobalVolume();
            if (aTime <= 0 || aTo == from)
            {
                setGlobalVolume(aTo);
                return;
            }
            mGlobalVolumeFader.set(from, aTo, aTime, mStreamTime);
        }

        /// <summary>
        /// Set up volume oscillator.
        /// </summary>
        public void oscillateVolume(Handle aVoiceHandle, float aFrom, float aTo, Time aTime)
        {
            if (aTime <= 0 || aTo == aFrom)
            {
                setVolume(aVoiceHandle, aTo);
                return;
            }

            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        ch.mVolumeFader.setLFO(aFrom, aTo, aTime, ch.mStreamTime);
                    }
                }
            }
        }

        /// <summary>
        /// Set up panning oscillator.
        /// </summary>
        public void oscillatePan(Handle aVoiceHandle, float aFrom, float aTo, Time aTime)
        {
            if (aTime <= 0 || aTo == aFrom)
            {
                setPan(aVoiceHandle, aTo);
                return;
            }

            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        ch.mPanFader.setLFO(aFrom, aTo, aTime, ch.mStreamTime);
                    }
                }
            }
        }

        /// <summary>
        /// Set up relative play speed oscillator.
        /// </summary>
        public void oscillateRelativePlaySpeed(Handle aVoiceHandle, float aFrom, float aTo, Time aTime)
        {
            if (aTime <= 0 || aTo == aFrom)
            {
                setRelativePlaySpeed(aVoiceHandle, aTo);
                return;
            }

            lock (mAudioThreadMutex)
            {
                ReadOnlySpan<Handle> h_ = VoiceGroupHandleToSpan(ref aVoiceHandle);
                foreach (Handle h in h_)
                {
                    AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                    if (ch != null)
                    {
                        ch.mRelativePlaySpeedFader.setLFO(aFrom, aTo, aTime, ch.mStreamTime);
                    }
                }
            }
        }

        /// <summary>
        /// Set up global volume oscillator.
        /// </summary>
        public void oscillateGlobalVolume(float aFrom, float aTo, Time aTime)
        {
            if (aTime <= 0 || aTo == aFrom)
            {
                setGlobalVolume(aTo);
                return;
            }
            mGlobalVolumeFader.setLFO(aFrom, aTo, aTime, mStreamTime);
        }
    }
}
