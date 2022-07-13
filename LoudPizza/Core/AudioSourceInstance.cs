using System;

namespace LoudPizza
{
    /// <summary>
    /// Base class for audio instances.
    /// </summary>
    public abstract unsafe class AudioSourceInstance : IAudioStream, IDisposable
    {
        [Flags]
        public enum FLAGS
        {
            /// <summary>
            /// This audio instance loops (if supported).
            /// </summary>
            LOOPING = 1,

            /// <summary>
            /// This audio instance is protected - won't get stopped if we run out of voices.
            /// </summary>
            PROTECTED = 2,

            /// <summary>
            /// This audio instance is paused.
            /// </summary>
            PAUSED = 4,

            /// <summary>
            /// This audio instance is affected by 3D processing.
            /// </summary>
            PROCESS_3D = 8,

            /// <summary>
            /// This audio instance has listener-relative 3D coordinates.
            /// </summary>
            LISTENER_RELATIVE = 16,

            /// <summary>
            /// Currently inaudible.
            /// </summary>
            INAUDIBLE = 32,

            /// <summary>
            /// If inaudible, should be killed (default = don't kill).
            /// </summary>
            INAUDIBLE_KILL = 64,

            /// <summary>
            /// If inaudible, should still be ticked (default = pause).
            /// </summary>
            INAUDIBLE_TICK = 128,

            /// <summary>
            /// Don't auto-stop sound.
            /// </summary>
            DISABLE_AUTOSTOP = 256
        }

        private bool _isDisposed;

        public AudioSourceInstance()
        {
            mPlayIndex = 0;
            mFlags = 0;
            mPan = 0;
            // Default all volumes to 1.0 so sound behind N mix busses isn't super quiet.
            int i;
            for (i = 0; i < SoLoud.MAX_CHANNELS; i++)
                mChannelVolume[i] = 1.0f;
            mSetVolume = 1.0f;
            mBaseSamplerate = 44100.0f;
            mSamplerate = 44100.0f;
            mSetRelativePlaySpeed = 1.0f;
            mStreamTime = 0.0f;
            mAudioSourceID = 0;
            mActiveFader = 0;
            mChannels = 1;
            mBusHandle = new Handle(~0u);
            mLoopCount = 0;
            mLoopPoint = 0;
            for (i = 0; i < mFilter.Length; i++)
            {
                mFilter[i] = null;
            }
            mCurrentChannelVolume = default;
            // behind pointers because we swap between the two buffers
            mResampleData0 = default;
            mResampleData1 = default;
            mSrcOffset = 0;
            mLeftoverSamples = 0;
            mDelaySamples = 0;
            mOverallVolume = 0;
            mOverallRelativePlaySpeed = 1;
        }

        /// <summary>
        /// Play index; used to identify instances from handles.
        /// </summary>
        public uint mPlayIndex;

        /// <summary>
        /// Loop count.
        /// </summary>
        public uint mLoopCount;

        public FLAGS mFlags;

        /// <summary>
        /// Pan value, for getPan().
        /// </summary>
        public float mPan;

        /// <summary>
        /// Volume for each channel (panning).
        /// </summary>
        public ChannelBuffer mChannelVolume;

        /// <summary>
        /// Set volume.
        /// </summary>
        public float mSetVolume;

        /// <summary>
        /// Overall volume overall = set * 3D.
        /// </summary>
        public float mOverallVolume;

        /// <summary>
        /// Base samplerate; samplerate = base samplerate * relative play speed.
        /// </summary>
        public float mBaseSamplerate;

        /// <summary>
        /// Samplerate; samplerate = base samplerate * relative play speed
        /// </summary>
        public float mSamplerate;

        /// <summary>
        /// Number of channels this audio source produces.
        /// </summary>
        public uint mChannels;

        /// <summary>
        /// Relative play speed; samplerate = base samplerate * relative play speed.
        /// </summary>
        public float mSetRelativePlaySpeed;

        /// <summary>
        /// Overall relative plays peed; overall = set * 3D.
        /// </summary>
        public float mOverallRelativePlaySpeed;

        /// <summary>
        /// How long this stream has played, in seconds.
        /// </summary>
        public Time mStreamTime;

        /// <summary>
        /// Position of this stream, in samples.
        /// </summary>
        public ulong mStreamPosition;

        /// <summary>
        /// Fader for the audio panning.
        /// </summary>
        public Fader mPanFader;

        /// <summary>
        /// Fader for the audio volume.
        /// </summary>
        public Fader mVolumeFader;

        /// <summary>
        /// Fader for the relative play speed.
        /// </summary>
        public Fader mRelativePlaySpeedFader;

        /// <summary>
        /// Fader used to schedule pausing of the stream.
        /// </summary>
        public Fader mPauseScheduler;

        /// <summary>
        /// Fader used to schedule stopping of the stream.
        /// </summary>
        public Fader mStopScheduler;

        /// <summary>
        /// Affected by some fader.
        /// </summary>
        public int mActiveFader;

        /// <summary>
        /// Current channel volumes, used to ramp the volume changes to avoid clicks.
        /// </summary>
        public ChannelBuffer mCurrentChannelVolume;

        /// <summary>
        /// ID of the sound source that generated this instance.
        /// </summary>
        public uint mAudioSourceID;

        /// <summary>
        /// Handle of the bus this audio instance is playing on. 0 for root.
        /// </summary>
        public Handle mBusHandle;

        /// <summary>
        /// Filters.
        /// </summary>
        public FilterInstance?[] mFilter = new FilterInstance[SoLoud.FILTERS_PER_STREAM];

        /// <summary>
        /// Initialize instance. Mostly internal use.
        /// </summary>
        public void init(AudioSource aSource, uint aPlayIndex)
        {
            mPlayIndex = aPlayIndex;
            mBaseSamplerate = aSource.mBaseSamplerate;
            mSamplerate = mBaseSamplerate;
            mChannels = aSource.mChannels;
            mStreamTime = 0.0f;
            mStreamPosition = 0;
            mLoopPoint = aSource.mLoopPoint;

            if ((aSource.mFlags & AudioSource.FLAGS.SHOULD_LOOP) != 0)
            {
                mFlags |= FLAGS.LOOPING;
            }
            if ((aSource.mFlags & AudioSource.FLAGS.PROCESS_3D) != 0)
            {
                mFlags |= FLAGS.PROCESS_3D;
            }
            if ((aSource.mFlags & AudioSource.FLAGS.LISTENER_RELATIVE) != 0)
            {
                mFlags |= FLAGS.LISTENER_RELATIVE;
            }
            if ((aSource.mFlags & AudioSource.FLAGS.INAUDIBLE_KILL) != 0)
            {
                mFlags |= FLAGS.INAUDIBLE_KILL;
            }
            if ((aSource.mFlags & AudioSource.FLAGS.INAUDIBLE_TICK) != 0)
            {
                mFlags |= FLAGS.INAUDIBLE_TICK;
            }
            if ((aSource.mFlags & AudioSource.FLAGS.DISABLE_AUTOSTOP) != 0)
            {
                mFlags |= FLAGS.DISABLE_AUTOSTOP;
            }
        }

        /// <summary>
        /// Buffer for the resampler.
        /// </summary>
        public AlignedFloatBuffer mResampleData0;

        /// <summary>
        /// Buffer for the resampler.
        /// </summary>
        public AlignedFloatBuffer mResampleData1;

        /// <summary>
        /// Sub-sample playhead; 16.16 fixed point.
        /// </summary>
        public uint mSrcOffset;

        /// <summary>
        /// Samples left over from earlier pass.
        /// </summary>
        public uint mLeftoverSamples;

        /// <summary>
        /// Number of samples to delay streaming.
        /// </summary>
        public uint mDelaySamples;

        /// <summary>
        /// When looping, start playing from this time.
        /// </summary>
        public ulong mLoopPoint;

        /// <summary>
        /// Get samples from the stream to the buffer.
        /// </summary>
        /// <returns>The amount of samples written.</returns>
        public abstract uint getAudio(float* aBuffer, uint aSamplesToRead, uint aBufferSize);

        /// <summary>
        /// Get whether the has stream ended.
        /// </summary>
        public abstract bool hasEnded();

        /// <summary>
        /// Seek to certain place in the stream. 
        /// </summary>
        /// <remarks>
        /// Base implementation is generic "tape" seek (and slow).
        /// </remarks>
        public abstract SOLOUD_ERRORS seek(ulong aSamplePosition, float* mScratch, uint mScratchSize);

        /// <summary>
        /// Get information. Returns 0 by default.
        /// </summary>
        public virtual float getInfo(uint aInfoKey)
        {
            return 0;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    for (int i = 0; i < mFilter.Length; i++)
                    {
                        mFilter[i]?.Dispose();
                    }
                }

                mResampleData0.destroy();
                mResampleData1.destroy();

                _isDisposed = true;
            }
        }

        ~AudioSourceInstance()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
