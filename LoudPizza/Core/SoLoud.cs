using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if SSE_INTRINSICS
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace LoudPizza
{
    public delegate void mutexCallFunction(object aMutexPtr);
    public delegate void soloudCallFunction(SoLoud aSoloud);

    public unsafe partial class SoLoud
    {
        private const int FIXPOINT_FRAC_BITS = 20;
        private const int FIXPOINT_FRAC_MUL = (1 << FIXPOINT_FRAC_BITS);
        private const int FIXPOINT_FRAC_MASK = ((1 << FIXPOINT_FRAC_BITS) - 1);

        private const float SQRT2RECP = 0.7071067811865475f;

        /// <summary>
        /// Maximum number of filters per stream.
        /// </summary>
        public const int FiltersPerStream = 8;

        /// <summary>
        /// Number of samples to process on one go.
        /// </summary>
        public const int SampleGranularity = 512;

        /// <summary>
        /// Maximum number of concurrent voices (hard limit is 4095).
        /// </summary>
        public const int MaxVoiceCount = 1024;

        /// <summary>
        /// 1) mono, 2) stereo, 4) quad, 6) 5.1, 8) 7.1,
        /// </summary>
        public const int MaxChannels = 8;

        /// <summary>
        /// Default resampler for both main and bus mixers.
        /// </summary>
        public const Resampler DefaultResampler = Resampler.Linear;

        /// <summary>
        /// Back-end data; content is up to the back-end implementation.
        /// </summary>
        private void* mBackendData;

        /// <summary>
        /// Pointer for the audio thread mutex.
        /// </summary>
        internal readonly object mAudioThreadMutex = new();

        /// <summary>
        /// Flag for when we're inside the mutex, used for debugging.
        /// </summary>
        private bool mInsideAudioThreadMutex;

        /// <summary>
        /// Called by <see cref="SoLoud"/> to shut down the back-end. 
        /// If <see langword="null"/>, not called. Should be set by back-end.
        /// </summary>
        public soloudCallFunction? mBackendCleanupFunc;

        //// CTor
        //Soloud();
        //// DTor
        //~Soloud();

        public enum Flags
        {
            /// <summary>
            /// Use round-off clipper.
            /// </summary>
            ClipRoundoff = 1,

            EnableVisualization = 2,

            LeftHanded3D = 4,

            [Obsolete("Not supported.")]
            NoFpuRegisterChange = 8,
        }

        public enum Waveform
        {
            Square = 0,
            Saw,
            Sin,
            Triangle,
            Bounce,
            Jaws,
            Humps,
            FSquare,
            FSaw,
        }

        public enum Resampler
        {
            Point,
            Linear,
            CatmullRom,
        }

        /// <summary>
        /// Initialize <see cref="SoLoud"/>. Must be called before <see cref="SoLoud"/> can be used.
        /// </summary>
        public SoLoud(Flags aFlags = Flags.ClipRoundoff)
        {
            mAudioThreadMutex = new object();

            mBackendString = null;

            mResampler = DefaultResampler;
            mInsideAudioThreadMutex = false;
            mScratchSize = 0;
            mSamplerate = 0;
            mBufferSize = 0;
            mFlags = 0;
            mGlobalVolume = 0;
            mPlayIndex = 0;
            mBackendData = null;
            mPostClipScaler = 0;
            mBackendCleanupFunc = null;
            mChannels = 2;
            mStreamTime = 0;
            mLastClockedTime = 0;
            mAudioSourceID = 1;
            mActiveVoiceDirty = true;
            mActiveVoiceCount = 0;
            int i;
            for (i = 0; i < MaxVoiceCount; i++)
                mActiveVoice[i] = 0;
            for (i = 0; i < FiltersPerStream; i++)
            {
                mFilter[i] = null;
                mFilterInstance[i] = null;
            }
            for (i = 0; i < 256; i++)
            {
                //mFFTData[i] = 0;
                mVisualizationWaveData[i] = 0;
                //mWaveData[i] = 0;
            }
            for (i = 0; i < MaxChannels; i++)
            {
                mVisualizationChannelVolume[i] = 0;
            }
            for (i = 0; i < MaxVoiceCount; i++)
            {
                mVoice[i] = null;
            }
            mVoiceGroupCount = 0;

            m3dPosition = default;
            m3dAt = new Vec3(0, 0, -1);
            m3dUp = new Vec3(0, 1, 0);
            m3dVelocity = default;
            m3dSoundSpeed = 343.3f;
            mMaxActiveVoices = 16;
            mHighestVoice = 0;
            mResampleData = null!;
            mResampleDataOwner = null!;
            for (i = 0; i < MaxChannels; i++)
                m3dSpeakerPosition[i] = default;
        }

        /// <summary>
        /// Deinitialize <see cref="SoLoud"/>. Must be called before shutting down.
        /// </summary>
        public void deinit()
        {
            Debug.Assert(!mInsideAudioThreadMutex);

            stopAll();

            mBackendCleanupFunc?.Invoke(this);
            mBackendCleanupFunc = null;
        }

        // Translate error number to an asciiz string
        public string getErrorString(SoLoudStatus aErrorCode)
        {
            switch (aErrorCode)
            {
                case SoLoudStatus.Ok:
                    return "No error";
                case SoLoudStatus.InvalidParameter:
                    return "Some parameter is invalid";
                case SoLoudStatus.FileNotFound:
                    return "File not found";
                case SoLoudStatus.FileLoadFailed:
                    return "File found, but could not be loaded";
                case SoLoudStatus.DllNotFound:
                    return "DLL not found, or wrong DLL";
                case SoLoudStatus.OutOfMemory:
                    return "Out of memory";
                case SoLoudStatus.NotImplemented:
                    return "Feature not implemented";
                default:
                    /*case UNKNOWN_ERROR: return "Other error";*/
                    return $"Unknown error ({aErrorCode})";
            }
        }

        // Calculate and get 256 floats of FFT data for visualization. Visualization has to be enabled before use.
        [SkipLocalsInit]
        public void calcFFT(out Buffer256 buffer)
        {
            float* temp = stackalloc float[1024];

            lock (mAudioThreadMutex)
            {
                for (int i = 0; i < 256; i++)
                {
                    temp[i * 2] = mVisualizationWaveData[i];
                    temp[i * 2 + 1] = 0;
                    temp[i + 512] = 0;
                    temp[i + 768] = 0;
                }
            }

            FFT.fft1024(temp);

            for (int i = 0; i < 256; i++)
            {
                float real = temp[i * 2];
                float imag = temp[i * 2 + 1];
                buffer[i] = MathF.Sqrt(real * real + imag * imag);
            }
        }

        // Get 256 floats of wave data for visualization. Visualization has to be enabled before use.
        public void getWave(out Buffer256 buffer)
        {
            lock (mAudioThreadMutex)
            {
                buffer = mVisualizationWaveData;
            }
        }

        // Get approximate output volume for a channel for visualization. Visualization has to be enabled before use.
        public float getApproximateVolume(uint aChannel)
        {
            if (aChannel > mChannels)
                return 0;

            lock (mAudioThreadMutex)
            {
                float vol = mVisualizationChannelVolume[aChannel];
                return vol;
            }
        }

        // Rest of the stuff is used internally.

        // Returns mixed float samples in buffer. Called by the back-end, or user with null driver.
        public void mix(float* aBuffer, uint aSamples)
        {
            uint stride = (aSamples + 15) & ~0xfu;
            mix_internal(aSamples, stride);
            interlace_samples_float(mScratch.mData, aBuffer, aSamples, mChannels, stride);
        }

        // Returns mixed 16-bit signed integer samples in buffer. Called by the back-end, or user with null driver.
        public void mixSigned16(short* aBuffer, uint aSamples)
        {
            uint stride = (aSamples + 15) & ~0xfu;
            mix_internal(aSamples, stride);
            interlace_samples_s16(mScratch.mData, aBuffer, aSamples, mChannels, stride);
        }



        // INTERNAL



        // Mix N samples * M channels. Called by other mix_ functions.
        internal void mix_internal(uint aSamples, uint aStride)
        {
            float buffertime = aSamples / (float)mSamplerate;
            mStreamTime += buffertime;
            mLastClockedTime = 0;

            float globalVolume0, globalVolume1;
            globalVolume0 = mGlobalVolume;
            if (mGlobalVolumeFader.mActive != 0)
            {
                mGlobalVolume = mGlobalVolumeFader.get(mStreamTime);
            }
            globalVolume1 = mGlobalVolume;

            lock (mAudioThreadMutex)
            {
                // Process faders. May change scratch size.
                for (uint i = 0; i < mHighestVoice; i++)
                {
                    AudioSourceInstance? voice = mVoice[i];
                    if (voice != null && ((voice.mFlags & AudioSourceInstance.Flags.Paused) == 0))
                    {
                        voice.mActiveFader = 0;

                        if (mGlobalVolumeFader.mActive > 0)
                        {
                            voice.mActiveFader = 1;
                        }

                        voice.mStreamTime += buffertime;
                        voice.mStreamPosition += (ulong)(aSamples * (double)voice.mOverallRelativePlaySpeed);

                        // TODO: this is actually unstable, because mStreamTime depends on the relative
                        // play speed. 
                        if (voice.mRelativePlaySpeedFader.mActive > 0)
                        {
                            float speed = voice.mRelativePlaySpeedFader.get(voice.mStreamTime);
                            setVoiceRelativePlaySpeed_internal(i, speed);
                        }

                        float volume0, volume1;
                        volume0 = voice.mOverallVolume;
                        if (voice.mVolumeFader.mActive > 0)
                        {
                            voice.mSetVolume = voice.mVolumeFader.get(voice.mStreamTime);
                            voice.mActiveFader = 1;
                            updateVoiceVolume_internal(i);
                            mActiveVoiceDirty = true;
                        }
                        volume1 = voice.mOverallVolume;

                        if (voice.mPanFader.mActive > 0)
                        {
                            float pan = voice.mPanFader.get(voice.mStreamTime);
                            setVoicePan_internal(i, pan);
                            voice.mActiveFader = 1;
                        }

                        if (voice.mPauseScheduler.mActive != Fader.ActiveFlags.Disabled)
                        {
                            voice.mPauseScheduler.get(voice.mStreamTime);
                            if (voice.mPauseScheduler.mActive == Fader.ActiveFlags.Inactive)
                            {
                                voice.mPauseScheduler.mActive = Fader.ActiveFlags.Disabled;
                                setVoicePause_internal(i, true);
                            }
                        }

                        if (voice.mStopScheduler.mActive != Fader.ActiveFlags.Disabled)
                        {
                            voice.mStopScheduler.get(voice.mStreamTime);
                            if (voice.mStopScheduler.mActive == Fader.ActiveFlags.Inactive)
                            {
                                voice.mStopScheduler.mActive = Fader.ActiveFlags.Disabled;
                                stopVoice_internal(i);
                            }
                        }
                    }
                }

                if (mActiveVoiceDirty)
                    calcActiveVoices_internal();

                mixBus_internal(mOutputScratch.mData, aSamples, aStride, mScratch.mData, default, mSamplerate, mChannels, mResampler);

                for (uint i = 0; i < FiltersPerStream; i++)
                {
                    FilterInstance? filterInstance = mFilterInstance[i];
                    if (filterInstance != null)
                    {
                        filterInstance.filter(mOutputScratch.mData, aSamples, aStride, mChannels, mSamplerate, mStreamTime);
                    }
                }
            }

            // Note: clipping channels*aStride, not channels*aSamples, so we're possibly clipping some unused data.
            // The buffers should be large enough for it, we just may do a few bytes of unneccessary work.
            clip_internal(mOutputScratch, mScratch, aStride, globalVolume0, globalVolume1);

            if ((mFlags & Flags.EnableVisualization) != 0)
            {
                for (nuint i = 0; i < MaxChannels; i++)
                {
                    mVisualizationChannelVolume[i] = 0;
                }

                float* scratch = mScratch.mData;
                if (aSamples > 255)
                {
                    for (nuint i = 0; i < 256; i++)
                    {
                        mVisualizationWaveData[i] = 0;
                        for (nuint j = 0; j < mChannels; j++)
                        {
                            float sample = scratch[i + j * aStride];
                            float absvol = MathF.Abs(sample);
                            if (mVisualizationChannelVolume[j] < absvol)
                                mVisualizationChannelVolume[j] = absvol;
                            mVisualizationWaveData[i] += sample;
                        }
                    }
                }
                else
                {
                    // Very unlikely failsafe branch
                    for (nuint i = 0; i < 256; i++)
                    {
                        mVisualizationWaveData[i] = 0;
                        for (nuint j = 0; j < mChannels; j++)
                        {
                            float sample = scratch[(i % aSamples) + j * aStride];
                            float absvol = MathF.Abs(sample);
                            if (mVisualizationChannelVolume[j] < absvol)
                                mVisualizationChannelVolume[j] = absvol;
                            mVisualizationWaveData[i] += sample;
                        }
                    }
                }
            }
        }

        public void initResampleData()
        {
            if (mResampleData != null)
            {
                for (uint i = 0; i < mResampleData.Length; i++)
                    mResampleData[i].destroy();
            }

            if (mResampleDataOwner != null)
            {
                for (uint i = 0; i < mResampleDataOwner.Length; i++)
                    mResampleDataOwner[i]?.Dispose();
            }

            mResampleData = new AlignedFloatBuffer[mMaxActiveVoices * 2];
            mResampleDataOwner = new AudioSourceInstance[mMaxActiveVoices];

            //mResampleDataBuffer.init(mMaxActiveVoices * 2 * SAMPLE_GRANULARITY * MAX_CHANNELS);

            for (uint i = 0; i < mMaxActiveVoices * 2; i++)
                mResampleData[i].init(SampleGranularity * MaxChannels);
            for (uint i = 0; i < mMaxActiveVoices; i++)
                mResampleDataOwner[i] = null;
        }

        /// <summary>
        /// Handle rest of initialization (called from backend).
        /// </summary>
        public void postinit_internal(uint aSamplerate, uint aBufferSize, Flags aFlags, uint aChannels)
        {
            mGlobalVolume = 1;
            mChannels = aChannels;
            mSamplerate = aSamplerate;
            mBufferSize = aBufferSize;
            mScratchSize = (aBufferSize + 15) & (~0xfu); // round to the next div by 16
            if (mScratchSize < SampleGranularity * 2)
                mScratchSize = SampleGranularity * 2;
            if (mScratchSize < 4096)
                mScratchSize = 4096;
            mScratch.init(mScratchSize * MaxChannels);
            mOutputScratch.init(mScratchSize * MaxChannels);
            initResampleData();
            mFlags = aFlags;
            mPostClipScaler = 0.95f;
            switch (mChannels)
            {
                case 1:
                    m3dSpeakerPosition[0] = new Vec3(0, 0, 1);
                    break;

                case 2:
                    m3dSpeakerPosition[0] = new Vec3(2, 0, 1);
                    m3dSpeakerPosition[1] = new Vec3(-2, 0, 1);
                    break;

                case 4:
                    m3dSpeakerPosition[0] = new Vec3(2, 0, 1);
                    m3dSpeakerPosition[1] = new Vec3(-2, 0, 1);
                    // I suppose technically the second pair should be straight left & right,
                    // but I prefer moving them a bit back to mirror the front speakers.
                    m3dSpeakerPosition[2] = new Vec3(2, 0, -1);
                    m3dSpeakerPosition[3] = new Vec3(-2, 0, -1);
                    break;

                case 6:
                    m3dSpeakerPosition[0] = new Vec3(2, 0, 1);
                    m3dSpeakerPosition[1] = new Vec3(-2, 0, 1);

                    // center and subwoofer. 
                    m3dSpeakerPosition[2] = new Vec3(0, 0, 1);
                    // Sub should be "mix of everything". We'll handle it as a special case and make it a null vector.
                    m3dSpeakerPosition[3] = new Vec3(0, 0, 0);

                    // I suppose technically the second pair should be straight left & right,
                    // but I prefer moving them a bit back to mirror the front speakers.
                    m3dSpeakerPosition[4] = new Vec3(2, 0, -2);
                    m3dSpeakerPosition[5] = new Vec3(-2, 0, -2);
                    break;

                case 8:
                    m3dSpeakerPosition[0] = new Vec3(2, 0, 1);
                    m3dSpeakerPosition[1] = new Vec3(-2, 0, 1);

                    // center and subwoofer. 
                    m3dSpeakerPosition[2] = new Vec3(0, 0, 1);
                    // Sub should be "mix of everything". We'll handle it as a special case and make it a null vector.
                    m3dSpeakerPosition[3] = new Vec3(0, 0, 0);

                    // side
                    m3dSpeakerPosition[4] = new Vec3(2, 0, 0);
                    m3dSpeakerPosition[5] = new Vec3(-2, 0, 0);

                    // back
                    m3dSpeakerPosition[6] = new Vec3(2, 0, -1);
                    m3dSpeakerPosition[7] = new Vec3(-2, 0, -1);
                    break;
            }
        }

        /// <summary>
        /// Update list of active voices.
        /// </summary>
        [SkipLocalsInit]
        internal void calcActiveVoices_internal()
        {
            // TODO: consider whether we need to re-evaluate the active voices all the time.
            // It is a must when new voices are started, but otherwise we could get away
            // with postponing it sometimes..

            mActiveVoiceDirty = false;

            // Populate
            uint candidates = 0;
            uint mustlive = 0;
            uint i;
            for (i = 0; i < mHighestVoice; i++)
            {
                AudioSourceInstance? voice = mVoice[i];
                if (voice != null &&
                    ((voice.mFlags & (AudioSourceInstance.Flags.Inaudible | AudioSourceInstance.Flags.Paused)) == 0 ||
                    ((voice.mFlags & AudioSourceInstance.Flags.InaudibleTick) != 0)))
                {
                    mActiveVoice[candidates] = i;
                    candidates++;
                    if ((voice.mFlags & AudioSourceInstance.Flags.InaudibleTick) != 0)
                    {
                        mActiveVoice[candidates - 1] = mActiveVoice[mustlive];
                        mActiveVoice[mustlive] = i;
                        mustlive++;
                    }
                }
            }

            // Check for early out
            if (candidates <= mMaxActiveVoices)
            {
                // everything is audible, early out
                mActiveVoiceCount = candidates;
                mapResampleBuffers_internal();
                return;
            }

            mActiveVoiceCount = mMaxActiveVoices;

            if (mustlive >= mMaxActiveVoices)
            {
                // Oopsie. Well, nothing to sort, since the "must live" voices already
                // ate all our active voice slots.
                // This is a potentially an error situation, but we have no way to report
                // error from here. And asserting could be bad, too.
                return;
            }

            // If we get this far, there's nothing to it: we'll have to sort the voices to find the most audible.

            // Iterative partial quicksort:
            int left = 0, pos = 0, right;
            uint* stack = stackalloc uint[24];
            uint len = candidates - mustlive;
            uint k = mActiveVoiceCount;
            for (; ; )
            {
                for (; left + 1 < len; len++)
                {
                    if (pos == 24)
                        len = stack[pos = 0];
                    uint pivot = mActiveVoice[left + mustlive];
                    float pivotvol = mVoice[pivot]!.mOverallVolume;
                    stack[pos++] = len;
                    for (right = left - 1; ;)
                    {
                        do
                        {
                            right++;
                        }
                        while (mVoice[mActiveVoice[right + mustlive]]!.mOverallVolume > pivotvol);
                        do
                        {
                            len--;
                        }
                        while (pivotvol > mVoice[mActiveVoice[len + mustlive]]!.mOverallVolume);
                        if (right >= len)
                            break;

                        uint temp = mActiveVoice[right + mustlive];
                        mActiveVoice[right + mustlive] = mActiveVoice[len + mustlive];
                        mActiveVoice[len + mustlive] = temp;
                    }
                }
                if (pos == 0)
                    break;
                if (left >= k)
                    break;
                left = (int)len;
                len = stack[--pos];
            }
            // TODO: should the rest of the voices be flagged INAUDIBLE?
            mapResampleBuffers_internal();
        }

        /// <summary>
        /// Map resample buffers to active voices.
        /// </summary>
        [SkipLocalsInit]
        internal void mapResampleBuffers_internal()
        {
            Debug.Assert(mMaxActiveVoices < 256);
            byte* live = stackalloc byte[256];
            CRuntime.memset(live, 0, mMaxActiveVoices);
            uint i, j;
            for (i = 0; i < mMaxActiveVoices; i++)
            {
                for (j = 0; j < mMaxActiveVoices; j++)
                {
                    if (mResampleDataOwner[i] != null && mResampleDataOwner[i] == mVoice[mActiveVoice[j]])
                    {
                        live[i] |= 1; // Live channel
                        live[j] |= 2; // Live voice
                    }
                }
            }

            for (i = 0; i < mMaxActiveVoices; i++)
            {
                AudioSourceInstance? owner = mResampleDataOwner[i];
                if ((live[i] & 1) == 0 && owner != null) // For all dead channels with owners..
                {
                    owner.mResampleData0.destroy();
                    owner.mResampleData1.destroy();
                    mResampleDataOwner[i] = null;
                }
            }

            int latestfree = 0;
            for (i = 0; i < mActiveVoiceCount; i++)
            {
                if ((live[i] & 2) == 0)
                {
                    AudioSourceInstance? foundInstance = mVoice[mActiveVoice[i]];
                    if (foundInstance != null) // For all live voices with no channel..
                    {
                        int found = -1;
                        for (j = (uint)latestfree; found == -1 && j < mMaxActiveVoices; j++)
                        {
                            if (mResampleDataOwner[j] == null)
                            {
                                found = (int)j;
                            }
                        }
                        Debug.Assert(found != -1);
                        mResampleDataOwner[found] = foundInstance;
                        foundInstance.mResampleData0 = mResampleData[found * 2 + 0];
                        foundInstance.mResampleData1 = mResampleData[found * 2 + 1];
                        foundInstance.mResampleData0.clear();
                        foundInstance.mResampleData1.clear();
                        latestfree = found + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Perform mixing for a specific bus.
        /// </summary>
        internal void mixBus_internal(
            float* aBuffer, uint aSamplesToRead, uint aBufferSize, float* aScratch,
            Handle aBus, float aSamplerate, uint aChannels, Resampler aResampler)
        {
            nuint i, j;
            // Clear accumulation buffer
            for (i = 0; i < aSamplesToRead; i++)
            {
                for (j = 0; j < aChannels; j++)
                {
                    aBuffer[i + j * aBufferSize] = 0;
                }
            }

            // Accumulate sound sources		
            for (i = 0; i < mActiveVoiceCount; i++)
            {
                AudioSourceInstance? voice = mVoice[mActiveVoice[i]];
                if (voice != null &&
                    voice.mBusHandle == aBus &&
                    (voice.mFlags & AudioSourceInstance.Flags.Paused) == 0 &&
                    (voice.mFlags & AudioSourceInstance.Flags.Inaudible) == 0)
                {
                    float step = voice.mSamplerate / aSamplerate;
                    // avoid step overflow
                    if (step > (1 << (32 - FIXPOINT_FRAC_BITS)))
                        step = 0;
                    uint step_fixed = (uint)(int)MathF.Floor(step * FIXPOINT_FRAC_MUL);
                    uint outofs = 0;

                    if (voice.mDelaySamples != 0)
                    {
                        if (voice.mDelaySamples > aSamplesToRead)
                        {
                            outofs = aSamplesToRead;
                            voice.mDelaySamples -= aSamplesToRead;
                        }
                        else
                        {
                            outofs = voice.mDelaySamples;
                            voice.mDelaySamples = 0;
                        }

                        // Clear scratch where we're skipping
                        uint k;
                        for (k = 0; k < voice.mChannels; k++)
                        {
                            CRuntime.memset(aScratch + k * aBufferSize, 0, sizeof(float) * outofs);
                        }
                    }

                    while (step_fixed != 0 && outofs < aSamplesToRead)
                    {
                        if (voice.mLeftoverSamples == 0)
                        {
                            // Swap resample buffers (ping-pong)
                            AlignedFloatBuffer t = voice.mResampleData0;
                            voice.mResampleData0 = voice.mResampleData1;
                            voice.mResampleData1 = t;

                            // Get a block of source data

                            uint readcount = 0;
                            if (!voice.hasEnded() || (voice.mFlags & AudioSourceInstance.Flags.Looping) != 0)
                            {
                                readcount = voice.getAudio(voice.mResampleData0.mData, SampleGranularity, SampleGranularity);
                                if (readcount < SampleGranularity)
                                {
                                    if ((voice.mFlags & AudioSourceInstance.Flags.Looping) != 0)
                                    {
                                        while (
                                            readcount < SampleGranularity &&
                                            voice.seek(voice.mLoopPoint, mScratch.mData, mScratchSize) == SoLoudStatus.Ok)
                                        {
                                            voice.mLoopCount++;
                                            uint inc = voice.getAudio(
                                                voice.mResampleData0.mData + readcount,
                                                SampleGranularity - readcount,
                                                SampleGranularity);

                                            readcount += inc;
                                            if (inc == 0)
                                                break;
                                        }
                                    }
                                }
                            }

                            // Clear remaining of the resample data if the full scratch wasn't used
                            if (readcount < SampleGranularity)
                            {
                                uint k;
                                for (k = 0; k < voice.mChannels; k++)
                                    CRuntime.memset(
                                        voice.mResampleData0.mData + readcount + SampleGranularity * k,
                                        0,
                                        sizeof(float) * (SampleGranularity - readcount));
                            }

                            // If we go past zero, crop to zero (a bit of a kludge)
                            if (voice.mSrcOffset < SampleGranularity * FIXPOINT_FRAC_MUL)
                            {
                                voice.mSrcOffset = 0;
                            }
                            else
                            {
                                // We have new block of data, move pointer backwards
                                voice.mSrcOffset -= SampleGranularity * FIXPOINT_FRAC_MUL;
                            }


                            // Run the per-stream filters to get our source data

                            for (j = 0; j < FiltersPerStream; j++)
                            {
                                FilterInstance? instance = voice.mFilter[j];
                                if (instance != null)
                                {
                                    instance.filter(
                                        voice.mResampleData0.mData,
                                        SampleGranularity,
                                        SampleGranularity,
                                        voice.mChannels,
                                        voice.mSamplerate,
                                        mStreamTime);
                                }
                            }
                        }
                        else
                        {
                            voice.mLeftoverSamples = 0;
                        }

                        // Figure out how many samples we can generate from this source data.
                        // The value may be zero.

                        uint writesamples = 0;

                        if (voice.mSrcOffset < SampleGranularity * FIXPOINT_FRAC_MUL)
                        {
                            writesamples = ((SampleGranularity * FIXPOINT_FRAC_MUL) - voice.mSrcOffset) / step_fixed + 1;

                            // avoid reading past the current buffer..
                            if (((writesamples * step_fixed + voice.mSrcOffset) >> FIXPOINT_FRAC_BITS) >= SampleGranularity)
                                writesamples--;
                        }


                        // If this is too much for our output buffer, don't write that many:
                        if (writesamples + outofs > aSamplesToRead)
                        {
                            voice.mLeftoverSamples = (writesamples + outofs) - aSamplesToRead;
                            writesamples = aSamplesToRead - outofs;
                        }

                        // Call resampler to generate the samples, once per channel
                        if (writesamples != 0)
                        {
                            for (j = 0; j < voice.mChannels; j++)
                            {
                                switch (aResampler)
                                {
                                    case Resampler.Point:
                                        resample_point(
                                            voice.mResampleData0.mData + SampleGranularity * j,
                                            voice.mResampleData1.mData + SampleGranularity * j,
                                            aScratch + aBufferSize * j + outofs,
                                            (int)voice.mSrcOffset,
                                            (int)writesamples,
                                            /*voice.mSamplerate,
                                            aSamplerate,*/
                                            (int)step_fixed);
                                        break;

                                    case Resampler.CatmullRom:
                                        resample_catmullrom(
                                            voice.mResampleData0.mData + SampleGranularity * j,
                                            voice.mResampleData1.mData + SampleGranularity * j,
                                            aScratch + aBufferSize * j + outofs,
                                            (int)voice.mSrcOffset,
                                            (int)writesamples,
                                            /*voice.mSamplerate,
                                            aSamplerate,*/
                                            (int)step_fixed);
                                        break;

                                    default:
                                        //case RESAMPLER.RESAMPLER_LINEAR:
                                        resample_linear(
                                            voice.mResampleData0.mData + SampleGranularity * j,
                                            voice.mResampleData1.mData + SampleGranularity * j,
                                            aScratch + aBufferSize * j + outofs,
                                            (int)voice.mSrcOffset,
                                            (int)writesamples,
                                            /*voice.mSamplerate,
                                            aSamplerate,*/
                                            (int)step_fixed);
                                        break;
                                }
                            }
                        }

                        // Keep track of how many samples we've written so far
                        outofs += writesamples;

                        // Move source pointer onwards (writesamples may be zero)
                        voice.mSrcOffset += writesamples * step_fixed;
                    }

                    // Handle panning and channel expansion (and/or shrinking)
                    panAndExpand(voice, aBuffer, aSamplesToRead, aBufferSize, aScratch, aChannels);

                    // clear voice if the sound is over
                    if ((voice.mFlags & (AudioSourceInstance.Flags.Looping | AudioSourceInstance.Flags.DisableAutostop)) == 0 &&
                        voice.hasEnded())
                    {
                        stopVoice_internal(mActiveVoice[i]);
                    }
                }
                else if (
                    voice != null &&
                    voice.mBusHandle == aBus &&
                    (voice.mFlags & AudioSourceInstance.Flags.Paused) == 0 &&
                    (voice.mFlags & AudioSourceInstance.Flags.Inaudible) != 0 &&
                    (voice.mFlags & AudioSourceInstance.Flags.InaudibleTick) != 0)
                {
                    // Inaudible but needs ticking. Do minimal work (keep counters up to date and ask audiosource for data)
                    float step = voice.mSamplerate / aSamplerate;
                    int step_fixed = (int)MathF.Floor(step * FIXPOINT_FRAC_MUL);
                    uint outofs = 0;

                    if (voice.mDelaySamples != 0)
                    {
                        if (voice.mDelaySamples > aSamplesToRead)
                        {
                            outofs = aSamplesToRead;
                            voice.mDelaySamples -= aSamplesToRead;
                        }
                        else
                        {
                            outofs = voice.mDelaySamples;
                            voice.mDelaySamples = 0;
                        }
                    }

                    while (step_fixed != 0 && outofs < aSamplesToRead)
                    {
                        if (voice.mLeftoverSamples == 0)
                        {
                            // Swap resample buffers (ping-pong)
                            AlignedFloatBuffer t = voice.mResampleData0;
                            voice.mResampleData0 = voice.mResampleData1;
                            voice.mResampleData1 = t;

                            // Get a block of source data

                            uint readcount = 0;
                            if (!voice.hasEnded() || (voice.mFlags & AudioSourceInstance.Flags.Looping) != 0)
                            {
                                readcount = voice.getAudio(voice.mResampleData0.mData, SampleGranularity, SampleGranularity);
                                if (readcount < SampleGranularity)
                                {
                                    if ((voice.mFlags & AudioSourceInstance.Flags.Looping) != 0)
                                    {
                                        while (
                                            readcount < SampleGranularity &&
                                            voice.seek(voice.mLoopPoint, mScratch.mData, mScratchSize) == SoLoudStatus.Ok)
                                        {
                                            voice.mLoopCount++;
                                            readcount += voice.getAudio(
                                                voice.mResampleData0.mData + readcount,
                                                SampleGranularity - readcount,
                                                SampleGranularity);
                                        }
                                    }
                                }
                            }

                            // If we go past zero, crop to zero (a bit of a kludge)
                            if (voice.mSrcOffset < SampleGranularity * FIXPOINT_FRAC_MUL)
                            {
                                voice.mSrcOffset = 0;
                            }
                            else
                            {
                                // We have new block of data, move pointer backwards
                                voice.mSrcOffset -= SampleGranularity * FIXPOINT_FRAC_MUL;
                            }

                            // Skip filters
                        }
                        else
                        {
                            voice.mLeftoverSamples = 0;
                        }

                        // Figure out how many samples we can generate from this source data.
                        // The value may be zero.

                        uint writesamples = 0;

                        if (voice.mSrcOffset < SampleGranularity * FIXPOINT_FRAC_MUL)
                        {
                            writesamples = ((SampleGranularity * FIXPOINT_FRAC_MUL) - voice.mSrcOffset) / (uint)step_fixed + 1;

                            // avoid reading past the current buffer..
                            if (((writesamples * step_fixed + voice.mSrcOffset) >> FIXPOINT_FRAC_BITS) >= SampleGranularity)
                                writesamples--;
                        }

                        // If this is too much for our output buffer, don't write that many:
                        if (writesamples + outofs > aSamplesToRead)
                        {
                            voice.mLeftoverSamples = (writesamples + outofs) - aSamplesToRead;
                            writesamples = aSamplesToRead - outofs;
                        }

                        // Skip resampler

                        // Keep track of how many samples we've written so far
                        outofs += writesamples;

                        // Move source pointer onwards (writesamples may be zero)
                        voice.mSrcOffset += (uint)(writesamples * step_fixed);
                    }

                    // clear voice if the sound is over
                    if ((voice.mFlags & (AudioSourceInstance.Flags.Looping | AudioSourceInstance.Flags.DisableAutostop)) == 0 &&
                        voice.hasEnded())
                    {
                        stopVoice_internal(mActiveVoice[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Clip the samples in the buffer.
        /// </summary>
        public void clip_internal(
            AlignedFloatBuffer aBuffer, AlignedFloatBuffer aDestBuffer, uint aSamples, float aVolume0, float aVolume1)
        {
#if SSE_INTRINSICS
            if (Sse.IsSupported)
            {
                float vd = (aVolume1 - aVolume0) / aSamples;
                float v = aVolume0;
                uint i, j, c, d;
                uint samplequads = (aSamples + 3) / 4; // rounded up

                // Clip
                if ((mFlags & Flags.ClipRoundoff) != 0)
                {
                    float nb = -1.65f;
                    Vector128<float> negbound = Vector128.Create(nb);
                    float pb = 1.65f;
                    Vector128<float> posbound = Vector128.Create(pb);
                    float ls = 0.87f;
                    Vector128<float> linearscale = Vector128.Create(ls);
                    float cs = -0.1f;
                    Vector128<float> cubicscale = Vector128.Create(cs);
                    float nw = -0.9862875f;
                    Vector128<float> negwall = Vector128.Create(nw);
                    float pw = 0.9862875f;
                    Vector128<float> poswall = Vector128.Create(pw);
                    Vector128<float> postscale = Vector128.Create(mPostClipScaler);
                    CRuntime.SkipInit(out TinyAlignedFloatBuffer volumes);
                    float* volumeData = TinyAlignedFloatBuffer.align(volumes.mData);
                    volumeData[0] = v;
                    volumeData[1] = v + vd;
                    volumeData[2] = v + vd + vd;
                    volumeData[3] = v + vd + vd + vd;
                    vd *= 4;
                    Vector128<float> vdelta = Vector128.Create(vd);
                    c = 0;
                    d = 0;
                    for (j = 0; j < mChannels; j++)
                    {
                        Vector128<float> vol = Sse.LoadAlignedVector128(volumeData);

                        for (i = 0; i < samplequads; i++)
                        {
                            //float f1 = origdata[c] * v;	c++; v += vd;
                            Vector128<float> f = Sse.LoadAlignedVector128(&aBuffer.mData[c]);
                            c += 4;
                            f = Sse.Multiply(f, vol);
                            vol = Sse.Add(vol, vdelta);

                            //float u1 = (f1 > -1.65f);
                            Vector128<float> u = Sse.CompareLessThan(f, negbound);

                            //float o1 = (f1 < 1.65f);
                            Vector128<float> o = Sse.CompareLessThan(f, posbound);

                            //f1 = (0.87f * f1 - 0.1f * f1 * f1 * f1) * u1 * o1;
                            Vector128<float> lin = Sse.Multiply(f, linearscale);
                            Vector128<float> cubic = Sse.Multiply(f, f);
                            cubic = Sse.Multiply(cubic, f);
                            cubic = Sse.Multiply(cubic, cubicscale);
                            f = Sse.Add(cubic, lin);

                            //f1 = f1 * u1 + !u1 * -0.9862875f;
                            Vector128<float> lowmask = Sse.AndNot(u, negwall);
                            Vector128<float> ilowmask = Sse.And(u, f);
                            f = Sse.Add(lowmask, ilowmask);

                            //f1 = f1 * o1 + !o1 * 0.9862875f;
                            Vector128<float> himask = Sse.AndNot(o, poswall);
                            Vector128<float> ihimask = Sse.And(o, f);
                            f = Sse.Add(himask, ihimask);

                            // outdata[d] = f1 * postclip; d++;
                            f = Sse.Multiply(f, postscale);
                            Sse.Store(&aDestBuffer.mData[d], f);
                            d += 4;
                        }
                    }
                }
                else
                {
                    float nb = -1.0f;
                    Vector128<float> negbound = Vector128.Create(nb);
                    float pb = 1.0f;
                    Vector128<float> posbound = Vector128.Create(pb);
                    Vector128<float> postscale = Vector128.Create(mPostClipScaler);
                    CRuntime.SkipInit(out TinyAlignedFloatBuffer volumes);
                    float* volumeData = TinyAlignedFloatBuffer.align(volumes.mData);
                    volumeData[0] = v;
                    volumeData[1] = v + vd;
                    volumeData[2] = v + vd + vd;
                    volumeData[3] = v + vd + vd + vd;
                    vd *= 4;
                    Vector128<float> vdelta = Vector128.Create(vd);
                    c = 0;
                    d = 0;
                    for (j = 0; j < mChannels; j++)
                    {
                        Vector128<float> vol = Sse.LoadAlignedVector128(volumeData);
                        for (i = 0; i < samplequads; i++)
                        {
                            //float f1 = aBuffer.mData[c] * v; c++; v += vd;
                            Vector128<float> f = Sse.LoadAlignedVector128(&aBuffer.mData[c]);
                            c += 4;
                            f = Sse.Multiply(f, vol);
                            vol = Sse.Add(vol, vdelta);

                            //f1 = (f1 <= -1) ? -1 : (f1 >= 1) ? 1 : f1;
                            f = Sse.Max(f, negbound);
                            f = Sse.Min(f, posbound);

                            //aDestBuffer.mData[d] = f1 * mPostClipScaler; d++;
                            f = Sse.Multiply(f, postscale);
                            Sse.Store(&aDestBuffer.mData[d], f);
                            d += 4;
                        }
                    }
                }
            }
            else
#endif // fallback code

            {
                float vd = (aVolume1 - aVolume0) / aSamples;
                float v = aVolume0;
                uint i, j, c, d;
                uint samplequads = (aSamples + 3) / 4; // rounded up
                                                       // Clip
                if ((mFlags & Flags.ClipRoundoff) != 0)
                {
                    c = 0;
                    d = 0;
                    for (j = 0; j < mChannels; j++)
                    {
                        v = aVolume0;
                        for (i = 0; i < samplequads; i++)
                        {
                            float f1 = aBuffer.mData[c] * v;
                            c++;
                            v += vd;
                            float f2 = aBuffer.mData[c] * v;
                            c++;
                            v += vd;
                            float f3 = aBuffer.mData[c] * v;
                            c++;
                            v += vd;
                            float f4 = aBuffer.mData[c] * v;
                            c++;
                            v += vd;

                            f1 = (f1 <= -1.65f) ? -0.9862875f : (f1 >= 1.65f) ? 0.9862875f : (0.87f * f1 - 0.1f * f1 * f1 * f1);
                            f2 = (f2 <= -1.65f) ? -0.9862875f : (f2 >= 1.65f) ? 0.9862875f : (0.87f * f2 - 0.1f * f2 * f2 * f2);
                            f3 = (f3 <= -1.65f) ? -0.9862875f : (f3 >= 1.65f) ? 0.9862875f : (0.87f * f3 - 0.1f * f3 * f3 * f3);
                            f4 = (f4 <= -1.65f) ? -0.9862875f : (f4 >= 1.65f) ? 0.9862875f : (0.87f * f4 - 0.1f * f4 * f4 * f4);

                            aDestBuffer.mData[d] = f1 * mPostClipScaler;
                            d++;
                            aDestBuffer.mData[d] = f2 * mPostClipScaler;
                            d++;
                            aDestBuffer.mData[d] = f3 * mPostClipScaler;
                            d++;
                            aDestBuffer.mData[d] = f4 * mPostClipScaler;
                            d++;
                        }
                    }
                }
                else
                {
                    c = 0;
                    d = 0;
                    for (j = 0; j < mChannels; j++)
                    {
                        v = aVolume0;
                        for (i = 0; i < samplequads; i++)
                        {
                            float f1 = aBuffer.mData[c] * v;
                            c++;
                            v += vd;
                            float f2 = aBuffer.mData[c] * v;
                            c++;
                            v += vd;
                            float f3 = aBuffer.mData[c] * v;
                            c++;
                            v += vd;
                            float f4 = aBuffer.mData[c] * v;
                            c++;
                            v += vd;

                            f1 = (f1 <= -1) ? -1 : (f1 >= 1) ? 1 : f1;
                            f2 = (f2 <= -1) ? -1 : (f2 >= 1) ? 1 : f2;
                            f3 = (f3 <= -1) ? -1 : (f3 >= 1) ? 1 : f3;
                            f4 = (f4 <= -1) ? -1 : (f4 >= 1) ? 1 : f4;

                            aDestBuffer.mData[d] = f1 * mPostClipScaler;
                            d++;
                            aDestBuffer.mData[d] = f2 * mPostClipScaler;
                            d++;
                            aDestBuffer.mData[d] = f3 * mPostClipScaler;
                            d++;
                            aDestBuffer.mData[d] = f4 * mPostClipScaler;
                            d++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Max. number of active voices. Busses and tickable inaudibles also count against this.
        /// </summary>
        private uint mMaxActiveVoices;

        /// <summary>
        /// Highest voice in use so far.
        /// </summary>
        public uint mHighestVoice;

        /// <summary>
        /// Scratch buffer, used for resampling.
        /// </summary>
        private AlignedFloatBuffer mScratch;

        /// <summary>
        /// Current size of the scratch, in samples.
        /// </summary>
        private uint mScratchSize;

        /// <summary>
        /// Output scratch buffer, used in mix_().
        /// </summary>
        private AlignedFloatBuffer mOutputScratch;

        /// <summary>
        /// Pointers to resampler buffers, two per active voice.
        /// </summary>
        private AlignedFloatBuffer[] mResampleData;

        // Actual allocated memory for resampler buffers
        //AlignedFloatBuffer mResampleDataBuffer;

        /// <summary>
        /// Owners of the resample data.
        /// </summary>
        private AudioSourceInstance?[] mResampleDataOwner;

        /// <summary>
        /// Audio voices.
        /// </summary>
        public AudioSourceInstance?[] mVoice = new AudioSourceInstance[MaxVoiceCount];

        /// <summary>
        /// Resampler for the main bus.
        /// </summary>
        private Resampler mResampler;

        /// <summary>
        /// Output sample rate (not float).
        /// </summary>
        private uint mSamplerate;

        /// <summary>
        /// Output channel count.
        /// </summary>
        private uint mChannels;

        /// <summary>
        /// Current backend string.
        /// </summary>
        public string? mBackendString;

        /// <summary>
        /// Maximum size of output buffer; used to calculate needed scratch.
        /// </summary>
        private uint mBufferSize;

        public Flags mFlags;

        /// <summary>
        /// Global volume. Applied before clipping.
        /// </summary>
        private float mGlobalVolume;

        /// <summary>
        /// Post-clip scaler. Applied after clipping.
        /// </summary>
        private float mPostClipScaler;

        /// <summary>
        /// Current play index. Used to create audio handles.
        /// </summary>
        private uint mPlayIndex;

        /// <summary>
        /// Current sound source index. Used to create sound source IDs.
        /// </summary>
        public uint mAudioSourceID;

        /// <summary>
        /// Fader for the global volume.
        /// </summary>
        private Fader mGlobalVolumeFader;

        /// <summary>
        /// Global stream time, for the global volume fader.
        /// </summary>
        private Time mStreamTime;

        /// <summary>
        /// Last time seen by the <see cref="playClocked"/> call.
        /// </summary>
        private Time mLastClockedTime;

        /// <summary>
        /// Global filters.
        /// </summary>
        private Filter?[] mFilter = new Filter[FiltersPerStream];

        /// <summary>
        /// Global filter instances.
        /// </summary>
        private FilterInstance?[] mFilterInstance = new FilterInstance[FiltersPerStream];

        /// <summary>
        /// Approximate volume for channels.
        /// </summary>
        private ChannelBuffer mVisualizationChannelVolume;

        /// <summary>
        /// Mono-mixed wave data for visualization and for visualization FFT input.
        /// </summary>
        private Buffer256 mVisualizationWaveData;

        // FFT output data
        //Buffer256 mFFTData;

        // Snapshot of wave data for visualization
        //Buffer256 mWaveData;

        /// <summary>
        /// 3D listener position.
        /// </summary>
        private Vec3 m3dPosition;

        /// <summary>
        /// 3D listener look-at.
        /// </summary>
        private Vec3 m3dAt;

        /// <summary>
        /// 3D listener up.
        /// </summary>
        private Vec3 m3dUp;

        /// <summary>
        /// 3D listener velocity.
        /// </summary>
        private Vec3 m3dVelocity;

        /// <summary>
        /// 3D speed of sound (for doppler).
        /// </summary>
        private float m3dSoundSpeed;

        /// <summary>
        /// 3D position of speakers.
        /// </summary>
        private Vec3[] m3dSpeakerPosition = new Vec3[MaxChannels];

        /// <summary>
        /// Data related to 3D processing, separate from AudioSource so we can do 3D calculations without audio mutex.
        /// </summary>
        private AudioSourceInstance3dData[] m3dData = new AudioSourceInstance3dData[MaxVoiceCount];

        /// <summary>
        /// Array of voice group arrays.
        /// </summary>
        private Handle[][] mVoiceGroup = Array.Empty<Handle[]>();

        private uint mVoiceGroupCount;

        /// <summary>
        /// List of currently active voices.
        /// </summary>
        private uint[] mActiveVoice = new uint[MaxVoiceCount];

        /// <summary>
        /// Number of currently active voices.
        /// </summary>
        private uint mActiveVoiceCount;

        /// <summary>
        /// Active voices list needs to be recalculated.
        /// </summary>
        private bool mActiveVoiceDirty;

        private static void interlace_samples_float(float* aSourceBuffer, float* aDestBuffer, uint aSamples, uint aChannels, uint aStride)
        {
            // 111222 -> 121212
            uint i, j, c;
            c = 0;
            for (j = 0; j < aChannels; j++)
            {
                c = j * aStride;
                for (i = j; i < aSamples * aChannels; i += aChannels)
                {
                    aDestBuffer[i] = aSourceBuffer[c];
                    c++;
                }
            }
        }

        private static void interlace_samples_s16(float* aSourceBuffer, short* aDestBuffer, uint aSamples, uint aChannels, uint aStride)
        {
            // 111222 -> 121212
            uint i, j, c;
            c = 0;
            for (j = 0; j < aChannels; j++)
            {
                c = j * aStride;
                for (i = j; i < aSamples * aChannels; i += aChannels)
                {
                    aDestBuffer[i] = (short)(aSourceBuffer[c] * 0x7fff);
                    c++;
                }
            }
        }

        private static float catmullrom(float t, float p0, float p1, float p2, float p3)
        {
            return 0.5f * (
                (2 * p1) +
                (-p0 + p2) * t +
                (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t +
                (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t
                );
        }

        private static void resample_catmullrom(float* aSrc,
            float* aSrc1,
            float* aDst,
            int aSrcOffset,
            int aDstSampleCount,
            int aStepFixed)
        {
            int i;
            int pos = aSrcOffset;

            for (i = 0; i < aDstSampleCount; i++, pos += aStepFixed)
            {
                int p = pos >> FIXPOINT_FRAC_BITS;
                int f = pos & FIXPOINT_FRAC_MASK;

                float s0, s1, s2, s3;

                if (p < 3)
                {
                    s3 = aSrc1[512 + p - 3];
                }
                else
                {
                    s3 = aSrc[p - 3];
                }

                if (p < 2)
                {
                    s2 = aSrc1[512 + p - 2];
                }
                else
                {
                    s2 = aSrc[p - 2];
                }

                if (p < 1)
                {
                    s1 = aSrc1[512 + p - 1];
                }
                else
                {
                    s1 = aSrc[p - 1];
                }

                s0 = aSrc[p];

                aDst[i] = catmullrom(f / (float)FIXPOINT_FRAC_MUL, s3, s2, s1, s0);
            }
        }

        private static void resample_linear(float* aSrc,
            float* aSrc1,
            float* aDst,
            int aSrcOffset,
            int aDstSampleCount,
            int aStepFixed)
        {
            int i;
            int pos = aSrcOffset;

            for (i = 0; i < aDstSampleCount; i++, pos += aStepFixed)
            {
                int p = pos >> FIXPOINT_FRAC_BITS;
                int f = pos & FIXPOINT_FRAC_MASK;
#if DEBUG
                if (p >= SampleGranularity || p < 0)
                {
                    // This should never actually happen
                    p = SampleGranularity - 1;
                }
#endif
                float s1 = aSrc1[SampleGranularity - 1];
                float s2 = aSrc[p];
                if (p != 0)
                {
                    s1 = aSrc[p - 1];
                }
                aDst[i] = s1 + (s2 - s1) * f * (1 / (float)FIXPOINT_FRAC_MUL);
            }
        }

        private static void resample_point(float* aSrc,
            float* aSrc1,
            float* aDst,
            int aSrcOffset,
            int aDstSampleCount,
            int aStepFixed)
        {
            int i;
            int pos = aSrcOffset;

            for (i = 0; i < aDstSampleCount; i++, pos += aStepFixed)
            {
                int p = pos >> FIXPOINT_FRAC_BITS;
                aDst[i] = aSrc[p];
            }
        }

        private void panAndExpand(
            AudioSourceInstance aVoice, float* aBuffer, uint aSamplesToRead, uint aBufferSize, float* aScratch, uint aChannels)
        {
#if SSE_INTRINSICS
            Debug.Assert(((nint)aBuffer & 0xf) == 0);
            Debug.Assert(((nint)aScratch & 0xf) == 0);
            Debug.Assert(((nint)aBufferSize & 0xf) == 0);
#endif
            ChannelBuffer pan; // current speaker volume
            ChannelBuffer pand; // destination speaker volume
            ChannelBuffer pani; // speaker volume increment per sample
            uint j, k;
            for (k = 0; k < aChannels; k++)
            {
                pan[k] = aVoice.mCurrentChannelVolume[k];
                pand[k] = aVoice.mChannelVolume[k] * aVoice.mOverallVolume;
                pani[k] = (pand[k] - pan[k]) / aSamplesToRead; // TODO: this is a bit inconsistent.. but it's a hack to begin with
            }

            uint ofs = 0;
            switch (aChannels)
            {
                case 1: // Target is mono. Sum everything. (1->1, 2->1, 4->1, 6->1, 8->1)
                    for (j = 0, ofs = 0; j < aVoice.mChannels; j++, ofs += aBufferSize)
                    {
                        pan[0] = aVoice.mCurrentChannelVolume[0];
                        for (k = 0; k < aSamplesToRead; k++)
                        {
                            pan[0] += pani[0];
                            aBuffer[k] += aScratch[ofs + k] * pan[0];
                        }
                    }
                    break;

                case 2:
                    switch (aVoice.mChannels)
                    {
                        case 8: // 8->2, just sum lefties and righties, add a bit of center and sub?
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                float s5 = aScratch[aBufferSize * 4 + j];
                                float s6 = aScratch[aBufferSize * 5 + j];
                                float s7 = aScratch[aBufferSize * 6 + j];
                                float s8 = aScratch[aBufferSize * 7 + j];
                                aBuffer[j + 0] += 0.2f * (s1 + s3 + s4 + s5 + s7) * pan[0];
                                aBuffer[j + aBufferSize] += 0.2f * (s2 + s3 + s4 + s6 + s8) * pan[1];
                            }
                            break;

                        case 6: // 6->2, just sum lefties and righties, add a bit of center and sub?
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                float s5 = aScratch[aBufferSize * 4 + j];
                                float s6 = aScratch[aBufferSize * 5 + j];
                                aBuffer[j + 0] += 0.3f * (s1 + s3 + s4 + s5) * pan[0];
                                aBuffer[j + aBufferSize] += 0.3f * (s2 + s3 + s4 + s6) * pan[1];
                            }
                            break;

                        case 4: // 4->2, just sum lefties and righties
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                aBuffer[j + 0] += 0.5f * (s1 + s3) * pan[0];
                                aBuffer[j + aBufferSize] += 0.5f * (s2 + s4) * pan[1];
                            }
                            break;

                        case 2: // 2->2
#if SSE_INTRINSICS
                            if (Sse.IsSupported)
                            {
                                uint c = 0;
                                //if ((aBufferSize & 3) == 0)
                                {
                                    uint samplequads = aSamplesToRead / 4; // rounded down
                                    CRuntime.SkipInit(out TinyAlignedFloatBuffer pan0);
                                    float* pan0Data = TinyAlignedFloatBuffer.align(pan0.mData);
                                    pan0Data[0] = pan[0] + pani[0];
                                    pan0Data[1] = pan[0] + pani[0] * 2;
                                    pan0Data[2] = pan[0] + pani[0] * 3;
                                    pan0Data[3] = pan[0] + pani[0] * 4;
                                    CRuntime.SkipInit(out TinyAlignedFloatBuffer pan1);
                                    float* pan1Data = TinyAlignedFloatBuffer.align(pan1.mData);
                                    pan1Data[0] = pan[1] + pani[1];
                                    pan1Data[1] = pan[1] + pani[1] * 2;
                                    pan1Data[2] = pan[1] + pani[1] * 3;
                                    pan1Data[3] = pan[1] + pani[1] * 4;
                                    pani[0] *= 4;
                                    pani[1] *= 4;
                                    Vector128<float> pan0delta = Vector128.Create(pani.Data[0]);
                                    Vector128<float> pan1delta = Vector128.Create(pani.Data[1]);
                                    Vector128<float> p0 = Sse.LoadAlignedVector128(pan0Data);
                                    Vector128<float> p1 = Sse.LoadAlignedVector128(pan1Data);

                                    for (j = 0; j < samplequads; j++)
                                    {
                                        Vector128<float> f0 = Sse.LoadAlignedVector128(aScratch + c);
                                        Vector128<float> c0 = Sse.Multiply(f0, p0);
                                        Vector128<float> f1 = Sse.LoadAlignedVector128(aScratch + c + aBufferSize);
                                        Vector128<float> c1 = Sse.Multiply(f1, p1);
                                        Vector128<float> o0 = Sse.LoadAlignedVector128(aBuffer + c);
                                        Vector128<float> o1 = Sse.LoadAlignedVector128(aBuffer + c + aBufferSize);
                                        c0 = Sse.Add(c0, o0);
                                        c1 = Sse.Add(c1, o1);
                                        Sse.Store(aBuffer + c, c0);
                                        Sse.Store(aBuffer + c + aBufferSize, c1);
                                        p0 = Sse.Add(p0, pan0delta);
                                        p1 = Sse.Add(p1, pan1delta);
                                        c += 4;
                                    }
                                }

                                // If buffer size or samples to read are not divisible by 4, handle leftovers
                                for (j = c; j < aSamplesToRead; j++)
                                {
                                    pan[0] += pani[0];
                                    pan[1] += pani[1];
                                    float s1 = aScratch[j];
                                    float s2 = aScratch[aBufferSize + j];
                                    aBuffer[j + 0] += s1 * pan[0];
                                    aBuffer[j + aBufferSize] += s2 * pan[1];
                                }
                            }
                            else
#endif
                            {
                                for (j = 0; j < aSamplesToRead; j++)
                                {
                                    pan[0] += pani[0];
                                    pan[1] += pani[1];
                                    float s1 = aScratch[j];
                                    float s2 = aScratch[aBufferSize + j];
                                    aBuffer[j + 0] += s1 * pan[0];
                                    aBuffer[j + aBufferSize] += s2 * pan[1];
                                }
                            }
                            break;

                        case 1: // 1->2
#if SSE_INTRINSICS
                            if (Sse.IsSupported)
                            {
                                uint c = 0;
                                //if ((aBufferSize & 3) == 0)
                                {
                                    uint samplequads = aSamplesToRead / 4; // rounded down
                                    CRuntime.SkipInit(out TinyAlignedFloatBuffer pan0);
                                    float* pan0Data = TinyAlignedFloatBuffer.align(pan0.mData);
                                    pan0Data[0] = pan[0] + pani[0];
                                    pan0Data[1] = pan[0] + pani[0] * 2;
                                    pan0Data[2] = pan[0] + pani[0] * 3;
                                    pan0Data[3] = pan[0] + pani[0] * 4;
                                    CRuntime.SkipInit(out TinyAlignedFloatBuffer pan1);
                                    float* pan1Data = TinyAlignedFloatBuffer.align(pan1.mData);
                                    pan1Data[0] = pan[1] + pani[1];
                                    pan1Data[1] = pan[1] + pani[1] * 2;
                                    pan1Data[2] = pan[1] + pani[1] * 3;
                                    pan1Data[3] = pan[1] + pani[1] * 4;
                                    pani[0] *= 4;
                                    pani[1] *= 4;
                                    Vector128<float> pan0delta = Vector128.Create(pani.Data[0]);
                                    Vector128<float> pan1delta = Vector128.Create(pani.Data[1]);
                                    Vector128<float> p0 = Sse.LoadAlignedVector128(pan0Data);
                                    Vector128<float> p1 = Sse.LoadAlignedVector128(pan1Data);

                                    for (j = 0; j < samplequads; j++)
                                    {
                                        Vector128<float> f = Sse.LoadAlignedVector128(aScratch + c);
                                        Vector128<float> c0 = Sse.Multiply(f, p0);
                                        Vector128<float> c1 = Sse.Multiply(f, p1);
                                        Vector128<float> o0 = Sse.LoadAlignedVector128(aBuffer + c);
                                        Vector128<float> o1 = Sse.LoadAlignedVector128(aBuffer + c + aBufferSize);
                                        c0 = Sse.Add(c0, o0);
                                        c1 = Sse.Add(c1, o1);
                                        Sse.Store(aBuffer + c, c0);
                                        Sse.Store(aBuffer + c + aBufferSize, c1);
                                        p0 = Sse.Add(p0, pan0delta);
                                        p1 = Sse.Add(p1, pan1delta);
                                        c += 4;
                                    }
                                }
                                // If buffer size or samples to read are not divisible by 4, handle leftovers
                                for (j = c; j < aSamplesToRead; j++)
                                {
                                    pan[0] += pani[0];
                                    pan[1] += pani[1];
                                    float s = aScratch[j];
                                    aBuffer[j + 0] += s * pan[0];
                                    aBuffer[j + aBufferSize] += s * pan[1];
                                }
                            }
                            else
#endif
                            {
                                for (j = 0; j < aSamplesToRead; j++)
                                {
                                    pan[0] += pani[0];
                                    pan[1] += pani[1];
                                    float s = aScratch[j];
                                    aBuffer[j + 0] += s * pan[0];
                                    aBuffer[j + aBufferSize] += s * pan[1];
                                }
                            }
                            break;
                    }
                    break;

                case 4:
                    switch (aVoice.mChannels)
                    {
                        case 8: // 8->4, add a bit of center, sub?
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                float s5 = aScratch[aBufferSize * 4 + j];
                                float s6 = aScratch[aBufferSize * 5 + j];
                                float s7 = aScratch[aBufferSize * 6 + j];
                                float s8 = aScratch[aBufferSize * 7 + j];
                                float c = (s3 + s4) * 0.7f;
                                aBuffer[j + 0] += s1 * pan[0] + c;
                                aBuffer[j + aBufferSize] += s2 * pan[1] + c;
                                aBuffer[j + aBufferSize * 2] += 0.5f * (s5 + s7) * pan[2];
                                aBuffer[j + aBufferSize * 3] += 0.5f * (s6 + s8) * pan[3];
                            }
                            break;

                        case 6: // 6->4, add a bit of center, sub?
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                float s5 = aScratch[aBufferSize * 4 + j];
                                float s6 = aScratch[aBufferSize * 5 + j];
                                float c = (s3 + s4) * 0.7f;
                                aBuffer[j + 0] += s1 * pan[0] + c;
                                aBuffer[j + aBufferSize] += s2 * pan[1] + c;
                                aBuffer[j + aBufferSize * 2] += s5 * pan[2];
                                aBuffer[j + aBufferSize * 3] += s6 * pan[3];
                            }
                            break;

                        case 4: // 4->4
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += s3 * pan[2];
                                aBuffer[j + aBufferSize * 3] += s4 * pan[3];
                            }
                            break;

                        case 2: // 2->4
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += s1 * pan[2];
                                aBuffer[j + aBufferSize * 3] += s2 * pan[3];
                            }
                            break;

                        case 1: // 1->4
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                float s = aScratch[j];
                                aBuffer[j + 0] += s * pan[0];
                                aBuffer[j + aBufferSize] += s * pan[1];
                                aBuffer[j + aBufferSize * 2] += s * pan[2];
                                aBuffer[j + aBufferSize * 3] += s * pan[3];
                            }
                            break;
                    }
                    break;

                case 6:
                    switch (aVoice.mChannels)
                    {
                        case 8: // 8->6
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                float s5 = aScratch[aBufferSize * 4 + j];
                                float s6 = aScratch[aBufferSize * 5 + j];
                                float s7 = aScratch[aBufferSize * 6 + j];
                                float s8 = aScratch[aBufferSize * 7 + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += s3 * pan[2];
                                aBuffer[j + aBufferSize * 3] += s4 * pan[3];
                                aBuffer[j + aBufferSize * 4] += 0.5f * (s5 + s7) * pan[4];
                                aBuffer[j + aBufferSize * 5] += 0.5f * (s6 + s8) * pan[5];
                            }
                            break;

                        case 6: // 6->6
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                float s5 = aScratch[aBufferSize * 4 + j];
                                float s6 = aScratch[aBufferSize * 5 + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += s3 * pan[2];
                                aBuffer[j + aBufferSize * 3] += s4 * pan[3];
                                aBuffer[j + aBufferSize * 4] += s5 * pan[4];
                                aBuffer[j + aBufferSize * 5] += s6 * pan[5];
                            }
                            break;

                        case 4: // 4->6
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += 0.5f * (s1 + s2) * pan[2];
                                aBuffer[j + aBufferSize * 3] += 0.25f * (s1 + s2 + s3 + s4) * pan[3];
                                aBuffer[j + aBufferSize * 4] += s3 * pan[4];
                                aBuffer[j + aBufferSize * 5] += s4 * pan[5];
                            }
                            break;

                        case 2: // 2->6
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += 0.5f * (s1 + s2) * pan[2];
                                aBuffer[j + aBufferSize * 3] += 0.5f * (s1 + s2) * pan[3];
                                aBuffer[j + aBufferSize * 4] += s1 * pan[4];
                                aBuffer[j + aBufferSize * 5] += s2 * pan[5];
                            }
                            break;

                        case 1: // 1->6
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                float s = aScratch[j];
                                aBuffer[j + 0] += s * pan[0];
                                aBuffer[j + aBufferSize] += s * pan[1];
                                aBuffer[j + aBufferSize * 2] += s * pan[2];
                                aBuffer[j + aBufferSize * 3] += s * pan[3];
                                aBuffer[j + aBufferSize * 4] += s * pan[4];
                                aBuffer[j + aBufferSize * 5] += s * pan[5];
                            }
                            break;
                    }
                    break;

                case 8:
                    switch (aVoice.mChannels)
                    {
                        case 8: // 8->8
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                pan[6] += pani[6];
                                pan[7] += pani[7];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                float s5 = aScratch[aBufferSize * 4 + j];
                                float s6 = aScratch[aBufferSize * 5 + j];
                                float s7 = aScratch[aBufferSize * 6 + j];
                                float s8 = aScratch[aBufferSize * 7 + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += s3 * pan[2];
                                aBuffer[j + aBufferSize * 3] += s4 * pan[3];
                                aBuffer[j + aBufferSize * 4] += s5 * pan[4];
                                aBuffer[j + aBufferSize * 5] += s6 * pan[5];
                                aBuffer[j + aBufferSize * 6] += s7 * pan[6];
                                aBuffer[j + aBufferSize * 7] += s8 * pan[7];
                            }
                            break;

                        case 6: // 6->8
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                pan[6] += pani[6];
                                pan[7] += pani[7];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                float s5 = aScratch[aBufferSize * 4 + j];
                                float s6 = aScratch[aBufferSize * 5 + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += s3 * pan[2];
                                aBuffer[j + aBufferSize * 3] += s4 * pan[3];
                                aBuffer[j + aBufferSize * 4] += 0.5f * (s5 + s1) * pan[4];
                                aBuffer[j + aBufferSize * 5] += 0.5f * (s6 + s2) * pan[5];
                                aBuffer[j + aBufferSize * 6] += s5 * pan[6];
                                aBuffer[j + aBufferSize * 7] += s6 * pan[7];
                            }
                            break;

                        case 4: // 4->8
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                pan[6] += pani[6];
                                pan[7] += pani[7];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                float s3 = aScratch[aBufferSize * 2 + j];
                                float s4 = aScratch[aBufferSize * 3 + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += 0.5f * (s1 + s2) * pan[2];
                                aBuffer[j + aBufferSize * 3] += 0.25f * (s1 + s2 + s3 + s4) * pan[3];
                                aBuffer[j + aBufferSize * 4] += 0.5f * (s1 + s3) * pan[4];
                                aBuffer[j + aBufferSize * 5] += 0.5f * (s2 + s4) * pan[5];
                                aBuffer[j + aBufferSize * 6] += s3 * pan[4];
                                aBuffer[j + aBufferSize * 7] += s4 * pan[5];
                            }
                            break;

                        case 2: // 2->8
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                pan[6] += pani[6];
                                pan[7] += pani[7];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                                aBuffer[j + aBufferSize * 2] += 0.5f * (s1 + s2) * pan[2];
                                aBuffer[j + aBufferSize * 3] += 0.5f * (s1 + s2) * pan[3];
                                aBuffer[j + aBufferSize * 4] += s1 * pan[4];
                                aBuffer[j + aBufferSize * 5] += s2 * pan[5];
                                aBuffer[j + aBufferSize * 6] += s1 * pan[6];
                                aBuffer[j + aBufferSize * 7] += s2 * pan[7];
                            }
                            break;

                        case 1: // 1->8
                            for (j = 0; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                pan[2] += pani[2];
                                pan[3] += pani[3];
                                pan[4] += pani[4];
                                pan[5] += pani[5];
                                pan[6] += pani[6];
                                pan[7] += pani[7];
                                float s = aScratch[j];
                                aBuffer[j + 0] += s * pan[0];
                                aBuffer[j + aBufferSize] += s * pan[1];
                                aBuffer[j + aBufferSize * 2] += s * pan[2];
                                aBuffer[j + aBufferSize * 3] += s * pan[3];
                                aBuffer[j + aBufferSize * 4] += s * pan[4];
                                aBuffer[j + aBufferSize * 5] += s * pan[5];
                                aBuffer[j + aBufferSize * 6] += s * pan[6];
                                aBuffer[j + aBufferSize * 7] += s * pan[7];
                            }
                            break;
                    }
                    break;
            }

            for (k = 0; k < aChannels; k++)
                aVoice.mCurrentChannelVolume[k] = pand[k];
        }
    }
}
