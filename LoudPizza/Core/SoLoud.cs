using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using LoudPizza.Sources;
using LoudPizza.Modifiers;
#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace LoudPizza.Core
{
    public delegate void mutexCallFunction(object aMutexPtr);
    public delegate void soloudCallFunction(SoLoud aSoloud);

    public unsafe partial class SoLoud
    {
        private const int FIXPOINT_FRAC_BITS = 20;
        private const int FIXPOINT_FRAC_MUL = (1 << FIXPOINT_FRAC_BITS);
        private const int FIXPOINT_FRAC_MASK = ((1 << FIXPOINT_FRAC_BITS) - 1);
        private const float FIXPOINT_FRAC_RECI = 1f / FIXPOINT_FRAC_MUL;

        private static readonly Vector<float> CONSECUTIVE_INDICES;

        public static readonly uint VECTOR_SIZE = (uint)Math.Max(
#if NET6_0_OR_GREATER
            Math.Max(Vector128<byte>.Count, Vector256<byte>.Count),
#else
            IntPtr.Size,
#endif
            Vector<byte>.Count);

        public static readonly uint VECTOR_ALIGNMENT = VECTOR_SIZE - 1;

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
        public const int MaxVoiceCount = 4095;

        /// <summary>
        /// 1) mono, 2) stereo, 4) quad, 6) 5.1, 8) 7.1,
        /// </summary>
        public const int MaxChannels = 8;

        /// <summary>
        /// Default resampler for both main and bus mixers.
        /// </summary>
        public static AudioResampler DefaultResampler { get; } = LinearAudioResampler.Instance;

        /// <summary>
        /// Back-end data; content is up to the back-end implementation.
        /// </summary>
        private void* mBackendData;

        /// <summary>
        /// Pointer for the audio thread mutex.
        /// </summary>
        internal readonly object mAudioThreadMutex = new();

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

        static SoLoud()
        {
            int* tmp = stackalloc int[Vector<int>.Count];
            for (int i = 0; i < Vector<int>.Count; i++)
            {
                tmp[i] = i + 1;
            }
            Vector<int> ivec = Unsafe.ReadUnaligned<Vector<int>>(tmp);
            CONSECUTIVE_INDICES = Vector.ConvertToSingle(ivec);
        }

        /// <summary>
        /// Initialize <see cref="SoLoud"/>. Must be called before <see cref="SoLoud"/> can be used.
        /// </summary>
        public SoLoud()
        {
            mAudioThreadMutex = new object();

            mBackendString = null;

            mResampler = DefaultResampler;
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
            mActiveVoice.AsSpan().Clear();
            mFilter.AsSpan().Clear();
            mFilterInstance.AsSpan().Clear();
            mVisualizationWaveData = default;
            mVisualizationChannelVolume = default;
            mVoice.AsSpan().Clear();

            m3dPosition = default;
            m3dAt = new Vector3(0, 0, -1);
            m3dUp = new Vector3(0, 1, 0);
            m3dVelocity = default;
            m3dSoundSpeed = 343.3f;
            mMaxActiveVoices = 16;
            mHighestVoice = 0;
            mResampleData = null!;
            mResampleDataOwners = null!;
            m3dSpeakerPosition.AsSpan().Clear();
        }

        /// <summary>
        /// Deinitialize <see cref="SoLoud"/>. Must be called before shutting down.
        /// </summary>
        public void deinit()
        {
            Debug.Assert(!Monitor.IsEntered(mAudioThreadMutex));

            stopAll();

            mBackendCleanupFunc?.Invoke(this);
            mBackendCleanupFunc = null;
        }

        /// <summary>
        /// Translate error number to a string.
        /// </summary>
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
                case SoLoudStatus.EndOfStream:
                    return "End of stream";
                case SoLoudStatus.PoolExhausted:
                    return "Pool exhausted";
                default:
                    /*case UNKNOWN_ERROR: return "Other error";*/
                    return $"Unknown error ({aErrorCode})";
            }
        }

        /// <inheritdoc/>
        [SkipLocalsInit]
        public void CalcFFT(out Buffer256 buffer)
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

        /// <inheritdoc/>
        public void GetWave(out Buffer256 buffer)
        {
            lock (mAudioThreadMutex)
            {
                buffer = mVisualizationWaveData;
            }
        }

        /// <inheritdoc/>
        public float GetApproximateVolume(uint aChannel)
        {
            if (aChannel > mChannels)
                return 0;

            lock (mAudioThreadMutex)
            {
                float vol = mVisualizationChannelVolume[aChannel];
                return vol;
            }
        }

        /// <inheritdoc/>
        public void GetApproximateVolumes(out ChannelBuffer buffer)
        {
            lock (mAudioThreadMutex)
            {
                buffer = mVisualizationChannelVolume;
            }
        }


        // Rest of the stuff is used internally.

        /// <summary>
        /// Returns mixed float samples in buffer. Called by the back-end, or user with null driver.
        /// </summary>
        public void mix(float* aBuffer, uint aSamples)
        {
            uint stride = (aSamples + VECTOR_ALIGNMENT) & ~VECTOR_ALIGNMENT;
            mix_internal(aSamples, stride);
            interlace_samples_float(mScratch.mData, aBuffer, aSamples, mChannels, stride);
        }

        /// <summary>
        /// Returns mixed 16-bit signed integer samples in buffer. Called by the back-end, or user with null driver.
        /// </summary>
        public void mixSigned16(short* aBuffer, uint aSamples)
        {
            uint stride = (aSamples + VECTOR_ALIGNMENT) & ~VECTOR_ALIGNMENT;
            mix_internal(aSamples, stride);
            interlace_samples_s16(mScratch.mData, aBuffer, aSamples, mChannels, stride);
        }



        // INTERNAL



        // Mix N samples * M channels. Called by other mix_ functions.
        internal void mix_internal(uint aSamples, uint aStride)
        {
            double dSamples = aSamples;
            double buffertime = dSamples / mSamplerate;
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
                ReadOnlySpan<AudioSourceInstance?> highVoices = mVoice.AsSpan(0, mHighestVoice);
                for (int i = 0; i < highVoices.Length; i++)
                {
                    AudioSourceInstance? voice = highVoices[i];
                    if (voice != null && ((voice.mFlags & AudioSourceInstance.Flags.Paused) == 0))
                    {
                        voice.mActiveFader = 0;

                        if (mGlobalVolumeFader.mActive > 0)
                        {
                            voice.mActiveFader = 1;
                        }

                        voice.mStreamTime += buffertime;
                        voice.mStreamPosition += (ulong)(dSamples * voice.mOverallRelativePlaySpeed);

                        // TODO: this is actually unstable, because mStreamTime depends on the relative
                        // play speed. 
                        if (voice.mRelativePlaySpeedFader.mActive > 0)
                        {
                            float speed = voice.mRelativePlaySpeedFader.get(voice.mStreamTime);
                            if (speed > 0)
                            {
                                setVoiceRelativePlaySpeed_internal(i, speed);
                            }
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

                        if (voice.mPauseScheduler.mActive != Fader.State.Disabled)
                        {
                            voice.mPauseScheduler.get(voice.mStreamTime);
                            if (voice.mPauseScheduler.mActive == Fader.State.Inactive)
                            {
                                voice.mPauseScheduler.mActive = Fader.State.Disabled;
                                setVoicePause_internal(i, true);
                            }
                        }

                        if (voice.mStopScheduler.mActive != Fader.State.Disabled)
                        {
                            voice.mStopScheduler.get(voice.mStreamTime);
                            if (voice.mStopScheduler.mActive == Fader.State.Inactive)
                            {
                                voice.mStopScheduler.mActive = Fader.State.Disabled;
                                stopVoice_internal(i);
                            }
                        }
                    }
                }

                if (mActiveVoiceDirty)
                    calcActiveVoices_internal();

                Span<float> outputScratch = mOutputScratch.AsSpan(0, (int)(mChannels * aStride));
                mixBus_internal(outputScratch, aSamples, aStride, mScratch.mData, default, mSamplerate, mChannels, mResampler);

                foreach (FilterInstance? filterInstance in mFilterInstance)
                {
                    if (filterInstance != null)
                    {
                        filterInstance.Filter(outputScratch, aSamples, aStride, mChannels, mSamplerate, mStreamTime);
                    }
                }
            }

            // Note: clipping channels*aStride, not channels*aSamples, so we're possibly clipping some unused data.
            // The buffers should be large enough for it, we just may do a few bytes of unneccessary work.
            clip_internal(mOutputScratch, mScratch, aStride, globalVolume0, globalVolume1);

            if ((mFlags & Flags.EnableVisualization) != 0)
            {
                mVisualizationChannelVolume = default;

                float* scratch = mScratch.mData;
                if (aSamples > 255)
                {
                    for (uint i = 0; i < 256; i++)
                    {
                        mVisualizationWaveData[i] = 0;
                        for (uint j = 0; j < mChannels; j++)
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
                    for (uint i = 0; i < 256; i++)
                    {
                        mVisualizationWaveData[i] = 0;
                        for (uint j = 0; j < mChannels; j++)
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

        internal void initResampleData()
        {
            foreach (ref AlignedFloatBuffer resampleData in mResampleData.AsSpan())
                resampleData.destroy();

            foreach (AudioSourceInstance? dataOwner in mResampleDataOwners.AsSpan())
                dataOwner?.Dispose();

            foreach (AudioSourceInstance? voice in mResampleDataOwners.AsSpan())
                voice?.Dispose();

            mResampleData = new AlignedFloatBuffer[mMaxActiveVoices * 2];
            mResampleDataOwners = new AudioSourceInstance[mMaxActiveVoices];
            mActiveVoice = new int[mMaxActiveVoices];
            mVoice = new AudioSourceInstance[mMaxActiveVoices];
            m3dData = new AudioSourceInstance3dData[mMaxActiveVoices];

            foreach (ref AlignedFloatBuffer resampleData in mResampleData.AsSpan())
            {
                resampleData.init(SampleGranularity * MaxChannels, VECTOR_SIZE);
            }
        }

        /// <summary>
        /// Handle rest of initialization (called from backend).
        /// </summary>
        public void postinit_internal(uint aSamplerate, uint aBufferSize, uint aChannels)
        {
            lock (mAudioThreadMutex)
            {
                mGlobalVolume = 1;
                mChannels = aChannels;
                mSamplerate = aSamplerate;
                mBufferSize = aBufferSize;
                uint mScratchSize = (aBufferSize + VECTOR_ALIGNMENT) & (~VECTOR_ALIGNMENT); // round to the next alignment
                if (mScratchSize < SampleGranularity * 2)
                    mScratchSize = SampleGranularity * 2;
                if (mScratchSize < 4096)
                    mScratchSize = 4096;
                mScratch.init(mScratchSize * MaxChannels, VECTOR_SIZE);
                mOutputScratch.init(mScratchSize * MaxChannels, VECTOR_SIZE);
                initResampleData();
                mPostClipScaler = 0.95f;
                switch (mChannels)
                {
                    case 1:
                        m3dSpeakerPosition[0] = new Vector3(0, 0, 1);
                        break;

                    case 2:
                        m3dSpeakerPosition[0] = new Vector3(2, 0, 1);
                        m3dSpeakerPosition[1] = new Vector3(-2, 0, 1);
                        break;

                    case 4:
                        m3dSpeakerPosition[0] = new Vector3(2, 0, 1);
                        m3dSpeakerPosition[1] = new Vector3(-2, 0, 1);
                        // I suppose technically the second pair should be straight left & right,
                        // but I prefer moving them a bit back to mirror the front speakers.
                        m3dSpeakerPosition[2] = new Vector3(2, 0, -1);
                        m3dSpeakerPosition[3] = new Vector3(-2, 0, -1);
                        break;

                    case 6:
                        m3dSpeakerPosition[0] = new Vector3(2, 0, 1);
                        m3dSpeakerPosition[1] = new Vector3(-2, 0, 1);

                        // center and subwoofer. 
                        m3dSpeakerPosition[2] = new Vector3(0, 0, 1);
                        // Sub should be "mix of everything". We'll handle it as a special case and make it a null vector.
                        m3dSpeakerPosition[3] = new Vector3(0, 0, 0);

                        // I suppose technically the second pair should be straight left & right,
                        // but I prefer moving them a bit back to mirror the front speakers.
                        m3dSpeakerPosition[4] = new Vector3(2, 0, -2);
                        m3dSpeakerPosition[5] = new Vector3(-2, 0, -2);
                        break;

                    case 8:
                        m3dSpeakerPosition[0] = new Vector3(2, 0, 1);
                        m3dSpeakerPosition[1] = new Vector3(-2, 0, 1);

                        // center and subwoofer. 
                        m3dSpeakerPosition[2] = new Vector3(0, 0, 1);
                        // Sub should be "mix of everything". We'll handle it as a special case and make it a null vector.
                        m3dSpeakerPosition[3] = new Vector3(0, 0, 0);

                        // side
                        m3dSpeakerPosition[4] = new Vector3(2, 0, 0);
                        m3dSpeakerPosition[5] = new Vector3(-2, 0, 0);

                        // back
                        m3dSpeakerPosition[6] = new Vector3(2, 0, -1);
                        m3dSpeakerPosition[7] = new Vector3(-2, 0, -1);
                        break;
                }
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

            ReadOnlySpan<AudioSourceInstance?> voices = mVoice.AsSpan();
            Span<int> activeVoices = mActiveVoice.AsSpan();

            mActiveVoiceDirty = false;

            // Populate
            int candidates = 0;
            int mustlive = 0;

            ReadOnlySpan<AudioSourceInstance?> highVoices = voices.Slice(0, mHighestVoice);
            for (int i = 0; i < highVoices.Length; i++)
            {
                AudioSourceInstance? voice = highVoices[i];
                if (voice != null &&
                    ((voice.mFlags & (AudioSourceInstance.Flags.Inaudible | AudioSourceInstance.Flags.Paused)) == 0 ||
                    ((voice.mFlags & AudioSourceInstance.Flags.InaudibleTick) != 0)))
                {
                    activeVoices[candidates] = i;
                    candidates++;
                    if ((voice.mFlags & AudioSourceInstance.Flags.InaudibleTick) != 0)
                    {
                        activeVoices[candidates - 1] = activeVoices[mustlive];
                        activeVoices[mustlive] = i;
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
            int* stack = stackalloc int[24];
            int len = candidates - mustlive;
            int k = mActiveVoiceCount;
            for (; ; )
            {
                for (; left + 1 < len; len++)
                {
                    if (pos == 24)
                        len = stack[pos = 0];
                    int pivot = activeVoices[left + mustlive];
                    float pivotvol = voices[pivot]!.mOverallVolume;
                    stack[pos++] = len;
                    for (right = left - 1; ;)
                    {
                        do
                        {
                            right++;
                        }
                        while (voices[activeVoices[right + mustlive]]!.mOverallVolume > pivotvol);
                        do
                        {
                            len--;
                        }
                        while (pivotvol > voices[activeVoices[len + mustlive]]!.mOverallVolume);
                        if (right >= len)
                            break;

                        int temp = activeVoices[right + mustlive];
                        activeVoices[right + mustlive] = activeVoices[len + mustlive];
                        activeVoices[len + mustlive] = temp;
                    }
                }
                if (pos == 0)
                    break;
                if (left >= k)
                    break;
                left = len;
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
            int maxActiveVoiceCount = mMaxActiveVoices;
            int activeVoiceCount = mActiveVoiceCount;
            Debug.Assert(maxActiveVoiceCount <= MaxVoiceCount);
            Debug.Assert(activeVoiceCount <= MaxVoiceCount);

            Span<AudioSourceInstance?> resampleDataOwners = mResampleDataOwners.AsSpan(0, maxActiveVoiceCount);
            ReadOnlySpan<AudioSourceInstance?> voices = mVoice.AsSpan(0, maxActiveVoiceCount);
            ReadOnlySpan<int> activeVoices = mActiveVoice.AsSpan(0, maxActiveVoiceCount);

            byte* live = stackalloc byte[MaxVoiceCount];
            new Span<byte>(live, maxActiveVoiceCount).Clear();

            for (int i = 0; i < maxActiveVoiceCount; i++)
            {
                for (int j = 0; j < maxActiveVoiceCount; j++)
                {
                    if (resampleDataOwners[i] != null &&
                        resampleDataOwners[i] == voices[activeVoices[j]])
                    {
                        live[i] |= 1; // Live channel
                        live[j] |= 2; // Live voice
                    }
                }
            }

            for (int i = 0; i < maxActiveVoiceCount; i++)
            {
                ref AudioSourceInstance? owner = ref resampleDataOwners[i];
                if ((live[i] & 1) == 0 && owner != null) // For all dead channels with owners..
                {
                    owner.mResampleData0 = -1;
                    owner.mResampleData1 = -1;
                    owner = null;
                }
            }

            int latestfree = 0;
            ReadOnlySpan<int> activeVoiceSlice = activeVoices.Slice(0, activeVoiceCount);
            for (int i = 0; i < activeVoiceSlice.Length; i++)
            {
                if ((live[i] & 2) == 0)
                {
                    AudioSourceInstance? foundInstance = voices[activeVoiceSlice[i]];
                    if (foundInstance != null) // For all live voices with no channel..
                    {
                        int found = -1;
                        for (int j = latestfree; found == -1 && j < maxActiveVoiceCount; j++)
                        {
                            if (resampleDataOwners[j] == null)
                            {
                                found = j;
                            }
                        }
                        Debug.Assert(found != -1);
                        resampleDataOwners[found] = foundInstance;
                        foundInstance.mResampleData0 = found * 2 + 0;
                        foundInstance.mResampleData1 = found * 2 + 1;
                        mResampleData[foundInstance.mResampleData0].AsSpan().Clear();
                        mResampleData[foundInstance.mResampleData1].AsSpan().Clear();
                        latestfree = found + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Perform mixing for a specific bus.
        /// </summary>
        internal void mixBus_internal(
            Span<float> aBuffer, uint aSamplesToRead, uint aBufferSize, float* aScratch,
            Handle aBus, float aSamplerate, uint aChannels, AudioResampler aResampler)
        {
            // Clear accumulation buffer
            aBuffer.Clear();

            // Accumulate sound sources		
            ReadOnlySpan<AudioSourceInstance?> voices = mVoice.AsSpan();
            ReadOnlySpan<AlignedFloatBuffer> resampleBuffers = mResampleData.AsSpan();
            ReadOnlySpan<int> activeVoices = mActiveVoice.AsSpan(0, mActiveVoiceCount);
            Span<float> scratch = mScratch.AsSpan();

            foreach (int activeVoice in activeVoices)
            {
                AudioSourceInstance? voice = voices[activeVoice];
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
                        for (uint k = 0; k < voice.Channels; k++)
                        {
                            new Span<float>(aScratch + k * aBufferSize, (int)outofs).Clear();
                        }
                    }

                    while (step_fixed != 0 && outofs < aSamplesToRead)
                    {
                        if (voice.mLeftoverSamples == 0)
                        {
                            voice.SwapResampleBuffers();
                            Span<float> resampleBuffer = resampleBuffers[voice.mResampleData0].AsSpan();

                            // Get a block of source data
                            uint readcount = 0;
                            if ((voice.mFlags & AudioSourceInstance.Flags.Looping) != 0 || !voice.HasEnded())
                            {
                                readcount = voice.GetAudio(resampleBuffer, SampleGranularity, SampleGranularity);
                                if (readcount < SampleGranularity)
                                {
                                    if ((voice.mFlags & AudioSourceInstance.Flags.Looping) != 0)
                                    {
                                        while (
                                            readcount < SampleGranularity &&
                                            voice.Seek(voice.mLoopPoint, scratch, out _) == SoLoudStatus.Ok)
                                        {
                                            voice.mLoopCount++;
                                            uint inc = voice.GetAudio(
                                                resampleBuffer.Slice((int)readcount),
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
                                for (uint k = 0; k < voice.Channels; k++)
                                {
                                    resampleBuffer.Slice(
                                        (int)(readcount + SampleGranularity * k),
                                        (int)(SampleGranularity - readcount)).Clear();
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

                            // Run the per-stream filters to get our source data
                            ReadOnlySpan<FilterInstance?> filters = voice.GetFilters();
                            foreach (FilterInstance? instance in filters)
                            {
                                if (instance != null)
                                {
                                    instance.Filter(
                                        resampleBuffer,
                                        SampleGranularity,
                                        SampleGranularity,
                                        voice.Channels,
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
                            ReadOnlySpan<float> resampleBuffer0 = resampleBuffers[voice.mResampleData0].AsSpan();
                            ReadOnlySpan<float> resampleBuffer1 = resampleBuffers[voice.mResampleData1].AsSpan();
                            Span<float> resampleScratch = new(aScratch, (int)(aBufferSize * aChannels));

                            uint channels = voice.Channels;
                            for (uint j = 0; j < channels; j++)
                            {
                                aResampler.Resample(
                                    resampleBuffer0.Slice((int)(SampleGranularity * j), SampleGranularity),
                                    resampleBuffer1.Slice((int)(SampleGranularity * j), SampleGranularity),
                                    resampleScratch.Slice((int)(aBufferSize * j + outofs), (int)writesamples),
                                    (int)voice.mSrcOffset,
                                    /*voice.mSamplerate,
                                    aSamplerate,*/
                                    (int)step_fixed);
                            }
                        }

                        // Keep track of how many samples we've written so far
                        outofs += writesamples;

                        // Move source pointer onwards (writesamples may be zero)
                        voice.mSrcOffset += writesamples * step_fixed;
                    }

                    // Handle panning and channel expansion (and/or shrinking)
                    fixed (float* bufferPtr = aBuffer)
                    {
                        panAndExpand(voice, bufferPtr, aSamplesToRead, aBufferSize, aScratch, aChannels);
                    }

                    // clear voice if the sound is over
                    if ((voice.mFlags & (AudioSourceInstance.Flags.Looping | AudioSourceInstance.Flags.DisableAutostop)) == 0 &&
                        voice.HasEnded())
                    {
                        stopVoice_internal(activeVoice);
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
                            voice.SwapResampleBuffers();
                            Span<float> resampleBuffer = resampleBuffers[voice.mResampleData0].AsSpan();

                            // Get a block of source data

                            if ((voice.mFlags & AudioSourceInstance.Flags.Looping) != 0 || !voice.HasEnded())
                            {
                                uint readcount = voice.GetAudio(resampleBuffer, SampleGranularity, SampleGranularity);
                                if (readcount < SampleGranularity)
                                {
                                    if ((voice.mFlags & AudioSourceInstance.Flags.Looping) != 0)
                                    {
                                        while (
                                            readcount < SampleGranularity &&
                                            voice.Seek(voice.mLoopPoint, scratch, out _) == SoLoudStatus.Ok)
                                        {
                                            voice.mLoopCount++;
                                            readcount += voice.GetAudio(
                                                resampleBuffer.Slice((int)readcount),
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
                        voice.HasEnded())
                    {
                        stopVoice_internal(activeVoice);
                    }
                }
            }
        }

        /// <summary>
        /// Clip the samples in the buffer.
        /// </summary>
        private void clip_internal(
            AlignedFloatBuffer aBuffer, AlignedFloatBuffer aDestBuffer, uint aSamples, float aVolume0, float aVolume1)
        {
#if NET6_0_OR_GREATER
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
                    Unsafe.SkipInit(out TinyAlignedFloatBuffer volumes);
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
                            Vector128<float> u = Sse.CompareGreaterThan(f, negbound);

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
                            Sse.StoreAligned(&aDestBuffer.mData[d], f);
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
                    Unsafe.SkipInit(out TinyAlignedFloatBuffer volumes);
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
                            Sse.StoreAligned(&aDestBuffer.mData[d], f);
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
        private int mMaxActiveVoices;

        /// <summary>
        /// Highest voice in use so far.
        /// </summary>
        internal int mHighestVoice;

        /// <summary>
        /// Scratch buffer, used for resampling.
        /// </summary>
        private AlignedFloatBuffer mScratch;

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
        private AudioSourceInstance?[] mResampleDataOwners = Array.Empty<AudioSourceInstance>();

        /// <summary>
        /// Audio voices.
        /// </summary>
        internal AudioSourceInstance?[] mVoice = Array.Empty<AudioSourceInstance>();

        /// <summary>
        /// Resampler for the main bus.
        /// </summary>
        private AudioResampler mResampler;

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

        private Flags mFlags;

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
        /// Current audio source index. Used to create audio source IDs.
        /// </summary>
        internal uint mAudioSourceID;

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

        /// <summary>
        /// 3D listener position.
        /// </summary>
        private Vector3 m3dPosition;

        /// <summary>
        /// 3D listener look-at.
        /// </summary>
        private Vector3 m3dAt;

        /// <summary>
        /// 3D listener up.
        /// </summary>
        private Vector3 m3dUp;

        /// <summary>
        /// 3D listener velocity.
        /// </summary>
        private Vector3 m3dVelocity;

        /// <summary>
        /// 3D speed of sound (for doppler).
        /// </summary>
        private float m3dSoundSpeed;

        /// <summary>
        /// 3D position of speakers.
        /// </summary>
        private Vector3[] m3dSpeakerPosition = new Vector3[MaxChannels];

        /// <summary>
        /// Data related to 3D processing, separate from AudioSource so we can do 3D calculations without audio mutex.
        /// </summary>
        private AudioSourceInstance3dData[] m3dData = Array.Empty<AudioSourceInstance3dData>();

        /// <summary>
        /// Array of voice group arrays.
        /// </summary>
        private Handle[][] mVoiceGroup = Array.Empty<Handle[]>();

        /// <summary>
        /// List of currently active voices.
        /// </summary>
        private int[] mActiveVoice = Array.Empty<int>();

        /// <summary>
        /// Number of currently active voices.
        /// </summary>
        private int mActiveVoiceCount;

        /// <summary>
        /// Active voices list needs to be recalculated.
        /// </summary>
        private bool mActiveVoiceDirty;

        private static void interlace_samples_float(float* aSourceBuffer, float* aDestBuffer, uint aSamples, uint aChannels, uint aStride)
        {
            // 111222 -> 121212

#if NET6_0_OR_GREATER
            if (Sse.IsSupported)
            {
                if (aChannels == 2)
                {
                    uint i = 0;
                    float* srcBuffer1 = aSourceBuffer;
                    float* srcBuffer2 = aSourceBuffer + aStride;

                    for (; i + (uint)Vector128<float>.Count * 2 <= aSamples * 2;)
                    {
                        Vector128<float> src0 = Sse.LoadAlignedVector128(srcBuffer1);
                        Vector128<float> src1 = Sse.LoadAlignedVector128(srcBuffer2);

                        Vector128<float> dst0 = Sse.UnpackLow(src0, src1);
                        Vector128<float> dst1 = Sse.UnpackHigh(src0, src1);

                        Unsafe.WriteUnaligned(aDestBuffer + i, dst0);
                        i += (uint)Vector128<float>.Count;

                        Unsafe.WriteUnaligned(aDestBuffer + i, dst1);
                        i += (uint)Vector128<float>.Count;

                        srcBuffer1 += Vector128<float>.Count;
                        srcBuffer2 += Vector128<float>.Count;
                    }

                    for (; i < aSamples * 2; i += 2)
                    {
                        aDestBuffer[i + 0] = *srcBuffer1++;
                        aDestBuffer[i + 1] = *srcBuffer2++;
                    }
                    return;
                }
            }
#endif

            for (uint j = 0; j < aChannels; j++)
            {
                uint i = j;
                uint c = j * aStride;

                for (; i < aSamples * aChannels; i += aChannels)
                {
                    aDestBuffer[i] = aSourceBuffer[c];
                    c++;
                }
            }
        }

        private static void interlace_samples_s16(float* aSourceBuffer, short* aDestBuffer, uint aSamples, uint aChannels, uint aStride)
        {
            // 111222 -> 121212

#if NET6_0_OR_GREATER
            if (Sse2.IsSupported)
            {
                if (aChannels == 2)
                {
                    uint i = 0;
                    float* srcBuffer1 = aSourceBuffer;
                    float* srcBuffer2 = aSourceBuffer + aStride;
                    Vector128<float> factor = Vector128.Create((float)0x7fff);

                    for (; i + (uint)Vector128<short>.Count * 2 <= aSamples * 2;)
                    {
                        Vector128<float> src0_0 = Sse.LoadAlignedVector128(srcBuffer1);
                        Vector128<float> src0_1 = Sse.LoadAlignedVector128(srcBuffer1 + Vector128<float>.Count);

                        Vector128<float> src1_0 = Sse.LoadAlignedVector128(srcBuffer2);
                        Vector128<float> src1_1 = Sse.LoadAlignedVector128(srcBuffer2 + Vector128<float>.Count);

                        Vector128<float> dst0 = Sse.Multiply(Sse.UnpackLow(src0_0, src1_0), factor);
                        Vector128<float> dst1 = Sse.Multiply(Sse.UnpackHigh(src0_0, src1_0), factor);

                        Vector128<float> dst2 = Sse.Multiply(Sse.UnpackLow(src0_1, src1_1), factor);
                        Vector128<float> dst3 = Sse.Multiply(Sse.UnpackHigh(src0_1, src1_1), factor);

                        Vector128<short> sdst0 = Sse2.PackSignedSaturate(
                            Sse2.ConvertToVector128Int32(dst0),
                            Sse2.ConvertToVector128Int32(dst1));

                        Vector128<short> sdst1 = Sse2.PackSignedSaturate(
                            Sse2.ConvertToVector128Int32(dst2),
                            Sse2.ConvertToVector128Int32(dst3));

                        Unsafe.WriteUnaligned(aDestBuffer + i, sdst0);
                        i += (uint)Vector128<short>.Count;

                        Unsafe.WriteUnaligned(aDestBuffer + i, sdst1);
                        i += (uint)Vector128<short>.Count;

                        srcBuffer1 += Vector128<short>.Count;
                        srcBuffer2 += Vector128<short>.Count;
                    }

                    for (; i < aSamples * 2; i += 2)
                    {
                        aDestBuffer[i + 0] = (short)(*srcBuffer1++ * 0x7fff);
                        aDestBuffer[i + 1] = (short)(*srcBuffer2++ * 0x7fff);
                    }
                    return;
                }
            }
#endif

            for (uint j = 0; j < aChannels; j++)
            {
                uint i = j;
                uint c = j * aStride;

                for (; i < aSamples * aChannels; i += aChannels)
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

        internal static void resample_catmullrom(
            float* aSrc0,
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
                    s3 = aSrc0[p - 3];
                }

                if (p < 2)
                {
                    s2 = aSrc1[512 + p - 2];
                }
                else
                {
                    s2 = aSrc0[p - 2];
                }

                if (p < 1)
                {
                    s1 = aSrc1[512 + p - 1];
                }
                else
                {
                    s1 = aSrc0[p - 1];
                }

                s0 = aSrc0[p];

                aDst[i] = catmullrom(f / (float)FIXPOINT_FRAC_MUL, s3, s2, s1, s0);
            }
        }

        internal static void resample_linear(
            float* aSrc0,
            float* aSrc1,
            float* aDst,
            int aSrcOffset,
            int aDstSampleCount,
            int aStepFixed)
        {
            int i = 0;
            int pos = aSrcOffset;

#if NET6_0_OR_GREATER
            if (Avx2.IsSupported)
            {
                Vector256<int> vPos = Vector256.Create(
                    pos + aStepFixed * 0,
                    pos + aStepFixed * 1,
                    pos + aStepFixed * 2,
                    pos + aStepFixed * 3,
                    pos + aStepFixed * 4,
                    pos + aStepFixed * 5,
                    pos + aStepFixed * 6,
                    pos + aStepFixed * 7);

                Vector256<int> vStepFixed = Vector256.Create(aStepFixed * 8);
                Vector256<int> fxpFracMask = Vector256.Create(FIXPOINT_FRAC_MASK);
                Vector256<float> fxpFracReci = Vector256.Create(FIXPOINT_FRAC_RECI);
                Vector256<float> s1BaseValue = Vector256.Create(aSrc1[SampleGranularity - 1]);
                Vector256<int> one = Vector256.Create(1);

                while (i + Vector256<float>.Count <= aDstSampleCount)
                {
                    Vector256<int> p = Avx2.ShiftRightArithmetic(vPos, FIXPOINT_FRAC_BITS);
                    Vector256<int> f = Avx2.And(vPos, fxpFracMask);

                    Vector256<int> mask = Avx2.CompareEqual(p, Vector256<int>.Zero);
                    mask = Avx2.Xor(mask, Vector256<int>.AllBitsSet); // bitwise-NOT

                    Vector256<int> pSub1 = Avx2.Subtract(p, one);
                    Vector256<float> s1 = Avx2.GatherMaskVector256(s1BaseValue, aSrc0, pSub1, mask.AsSingle(), sizeof(float));
                    Vector256<float> s2 = Avx2.GatherVector256(aSrc0, p, sizeof(float));

                    Vector256<float> diff = Avx.Multiply(Avx.Subtract(s2, s1), Avx.ConvertToVector256Single(f));
                    Vector256<float> dst = Fma.IsSupported
                        ? Fma.MultiplyAdd(diff, fxpFracReci, s1)
                        : Avx.Add(Avx.Multiply(diff, fxpFracReci), s1);

                    Unsafe.WriteUnaligned(aDst + i, dst);

                    i += Vector256<float>.Count;
                    vPos = Avx2.Add(vPos, vStepFixed);
                }

                pos = vPos.GetElement(0);
            }
#endif

            for (; i < aDstSampleCount; i++, pos += aStepFixed)
            {
                int p = pos >> FIXPOINT_FRAC_BITS;
                int f = pos & FIXPOINT_FRAC_MASK;
#if DEBUG
                if ((uint)p >= SampleGranularity)
                {
                    // This should never actually happen
                    p = SampleGranularity - 1;
                }
#endif
                float s1 = aSrc1[SampleGranularity - 1];
                float s2 = aSrc0[p];
                if (p != 0)
                {
                    s1 = aSrc0[p - 1];
                }
                aDst[i] = s1 + (s2 - s1) * f * FIXPOINT_FRAC_RECI;
            }
        }

        internal static void resample_point(
            float* aSrc0,
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
                aDst[i] = aSrc0[p];
            }
        }

        private void panAndExpand(
            AudioSourceInstance aVoice, float* aBuffer, uint aSamplesToRead, uint aBufferSize, float* aScratch, uint aChannels)
        {
            Debug.Assert(((nuint)aScratch & VECTOR_ALIGNMENT) == 0);
            Debug.Assert(((nuint)aBufferSize & VECTOR_ALIGNMENT) == 0);

            ChannelBuffer pan; // current speaker volume
            ChannelBuffer pand; // destination speaker volume
            ChannelBuffer pani; // speaker volume increment per sample

            for (uint k = 0; k < aChannels; k++)
            {
                pan[k] = aVoice.mCurrentChannelVolume[k];
                pand[k] = aVoice.mChannelVolume[k] * aVoice.mOverallVolume;
                pani[k] = (pand[k] - pan[k]) / aSamplesToRead; // TODO: this is a bit inconsistent.. but it's a hack to begin with
            }

            uint voiceChannels = aVoice.Channels;
            uint j = 0;
            switch (aChannels)
            {
                case 1: // Target is mono. Sum everything. (1->1, 2->1, 4->1, 6->1, 8->1)
                    for (uint ofs = 0; j < voiceChannels; j++, ofs += aBufferSize)
                    {
                        pan[0] = aVoice.mCurrentChannelVolume[0];
                        for (uint k = 0; k < aSamplesToRead; k++)
                        {
                            pan[0] += pani[0];
                            aBuffer[k] += aScratch[ofs + k] * pan[0];
                        }
                    }
                    break;

                case 2:
                    switch (voiceChannels)
                    {
                        case 8: // 8->2, just sum lefties and righties, add a bit of center and sub?
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            if (Vector.IsHardwareAccelerated)
                            {
                                uint samplequads = aSamplesToRead / (uint)Vector<float>.Count; // rounded down
                                Vector<float> indices = CONSECUTIVE_INDICES;
                                Vector<float> p0 = new Vector<float>(pan[0]) + new Vector<float>(pani[0]) * indices;
                                Vector<float> p1 = new Vector<float>(pan[1]) + new Vector<float>(pani[1]) * indices;

                                Vector<float> pan0delta = new(pani[0] * Vector<float>.Count);
                                Vector<float> pan1delta = new(pani[1] * Vector<float>.Count);

                                for (uint q = 0; q < samplequads; q++)
                                {
                                    Vector<float> f0 = Unsafe.Read<Vector<float>>(aScratch + j);
                                    Vector<float> f1 = Unsafe.Read<Vector<float>>(aScratch + j + aBufferSize);
                                    Vector<float> o0 = Unsafe.ReadUnaligned<Vector<float>>(aBuffer + j);
                                    Vector<float> o1 = Unsafe.ReadUnaligned<Vector<float>>(aBuffer + j + aBufferSize);

                                    Vector<float> c0 = f0 * p0 + o0;
                                    Vector<float> c1 = f1 * p1 + o1;
                                    Unsafe.WriteUnaligned(aBuffer + j, c0);
                                    Unsafe.WriteUnaligned(aBuffer + j + aBufferSize, c1);

                                    p0 += pan0delta;
                                    p1 += pan1delta;
                                    j += (uint)Vector<float>.Count;
                                }
                            }

                            // If buffer size or samples to read are not divisible by vector length, handle leftovers
                            for (; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                float s1 = aScratch[j];
                                float s2 = aScratch[aBufferSize + j];
                                aBuffer[j + 0] += s1 * pan[0];
                                aBuffer[j + aBufferSize] += s2 * pan[1];
                            }
                            break;

                        case 1: // 1->2
                            if (Vector.IsHardwareAccelerated)
                            {
                                uint samplequads = aSamplesToRead / (uint)Vector<float>.Count; // rounded down
                                Vector<float> indices = CONSECUTIVE_INDICES;
                                Vector<float> p0 = new Vector<float>(pan[0]) + new Vector<float>(pani[0]) * indices;
                                Vector<float> p1 = new Vector<float>(pan[1]) + new Vector<float>(pani[1]) * indices;

                                Vector<float> pan0delta = new(pani[0] * Vector<float>.Count);
                                Vector<float> pan1delta = new(pani[1] * Vector<float>.Count);

                                for (uint q = 0; q < samplequads; q++)
                                {
                                    Vector<float> f0 = Unsafe.Read<Vector<float>>(aScratch + j);
                                    Vector<float> o0 = Unsafe.ReadUnaligned<Vector<float>>(aBuffer + j);
                                    Vector<float> o1 = Unsafe.ReadUnaligned<Vector<float>>(aBuffer + j + aBufferSize);

                                    Vector<float> c0 = f0 * p0 + o0;
                                    Vector<float> c1 = f0 * p1 + o1;
                                    Unsafe.WriteUnaligned(aBuffer + j, c0);
                                    Unsafe.WriteUnaligned(aBuffer + j + aBufferSize, c1);

                                    p0 += pan0delta;
                                    p1 += pan1delta;
                                    j += (uint)Vector<float>.Count;
                                }
                            }

                            // If buffer size or samples to read are not divisible by vector length, handle leftovers
                            for (; j < aSamplesToRead; j++)
                            {
                                pan[0] += pani[0];
                                pan[1] += pani[1];
                                float s = aScratch[j];
                                aBuffer[j + 0] += s * pan[0];
                                aBuffer[j + aBufferSize] += s * pan[1];
                            }
                            break;
                    }
                    break;

                case 4:
                    switch (voiceChannels)
                    {
                        case 8: // 8->4, add a bit of center, sub?
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                    switch (voiceChannels)
                    {
                        case 8: // 8->6
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                    switch (voiceChannels)
                    {
                        case 8: // 8->8
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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
                            for (; j < aSamplesToRead; j++)
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

            for (uint k = 0; k < aChannels; k++)
                aVoice.mCurrentChannelVolume[k] = pand[k];
        }
    }
}
