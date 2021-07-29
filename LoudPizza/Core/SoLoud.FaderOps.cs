using System;

namespace LoudPizza
{
    public unsafe partial class SoLoud
    {
        // Schedule a stream to pause
        public void schedulePause(Handle aVoiceHandle, Time aTime)
        {
            if (aTime <= 0)
            {
                setPause(aVoiceHandle, true);
                return;
            }

            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mPauseScheduler.set(1, 0, aTime, ch.mStreamTime);
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

        // Schedule a stream to stop
        public void scheduleStop(Handle aVoiceHandle, Time aTime)
        {
            if (aTime <= 0)
            {
                stop(aVoiceHandle);
                return;
            }

            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mStopScheduler.set(1, 0, aTime, ch.mStreamTime);
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

        // Set up volume fader
        public void fadeVolume(Handle aVoiceHandle, float aTo, Time aTime)
        {
            float from = getVolume(aVoiceHandle);
            if (aTime <= 0 || aTo == from)
            {
                setVolume(aVoiceHandle, aTo);
                return;
            }

            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mVolumeFader.set(from, aTo, aTime, ch.mStreamTime);
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

        // Set up panning fader
        public void fadePan(Handle aVoiceHandle, float aTo, Time aTime)
        {
            float from = getPan(aVoiceHandle);
            if (aTime <= 0 || aTo == from)
            {
                setPan(aVoiceHandle, aTo);
                return;
            }

            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mPanFader.set(from, aTo, aTime, ch.mStreamTime);
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

        // Set up relative play speed fader
        public void fadeRelativePlaySpeed(Handle aVoiceHandle, float aTo, Time aTime)
        {
            float from = getRelativePlaySpeed(aVoiceHandle);
            if (aTime <= 0 || aTo == from)
            {
                setRelativePlaySpeed(aVoiceHandle, aTo);
                return;
            }

            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mRelativePlaySpeedFader.set(from, aTo, aTime, ch.mStreamTime);
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

        // Set up global volume fader
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

        // Set up volume oscillator
        public void oscillateVolume(Handle aVoiceHandle, float aFrom, float aTo, Time aTime)
        {
            if (aTime <= 0 || aTo == aFrom)
            {
                setVolume(aVoiceHandle, aTo);
                return;
            }

            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mVolumeFader.setLFO(aFrom, aTo, aTime, ch.mStreamTime);
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

        // Set up panning oscillator
        public void oscillatePan(Handle aVoiceHandle, float aFrom, float aTo, Time aTime)
        {
            if (aTime <= 0 || aTo == aFrom)
            {
                setPan(aVoiceHandle, aTo);
                return;
            }

            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mPanFader.setLFO(aFrom, aTo, aTime, ch.mStreamTime);
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

        // Set up relative play speed oscillator
        public void oscillateRelativePlaySpeed(Handle aVoiceHandle, float aFrom, float aTo, Time aTime)
        {
            if (aTime <= 0 || aTo == aFrom)
            {
                setRelativePlaySpeed(aVoiceHandle, aTo);
                return;
            }

            void body(Handle h)
            {
                AudioSourceInstance? ch = getVoiceRefFromHandle_internal(h);
                if (ch != null)
                {
                    ch.mRelativePlaySpeedFader.setLFO(aFrom, aTo, aTime, ch.mStreamTime);
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

        // Set up global volume oscillator
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
