using System;

namespace LoudPizza
{
    // Base class for audio instances
    public abstract unsafe class AudioSourceInstance : IAudioStream, IDisposable
    {
        [Flags]
        public enum FLAGS
        {
            // This audio instance loops (if supported)
            LOOPING = 1,
            // This audio instance is protected - won't get stopped if we run out of voices
            PROTECTED = 2,
            // This audio instance is paused
            PAUSED = 4,
            // This audio instance is affected by 3d processing
            PROCESS_3D = 8,
            // This audio instance has listener-relative 3d coordinates
            LISTENER_RELATIVE = 16,
            // Currently inaudible
            INAUDIBLE = 32,
            // If inaudible, should be killed (default = don't kill kill)
            INAUDIBLE_KILL = 64,
            // If inaudible, should still be ticked (default = pause)
            INAUDIBLE_TICK = 128,
            // Don't auto-stop sound
            DISABLE_AUTOSTOP = 256
        }

        private bool _isDisposed;

        // Ctor
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

        // Play index; used to identify instances from handles
        public uint mPlayIndex;

        // Loop count
        public uint mLoopCount;

        // Flags; see AudioSourceInstance.FLAGS.FLAGS
        public FLAGS mFlags;

        // Pan value, for getPan()
        public float mPan;

        // Volume for each channel (panning)
        public ChannelBuffer mChannelVolume;

        // Set volume
        public float mSetVolume;

        // Overall volume overall = set * 3d
        public float mOverallVolume;

        // Base samplerate; samplerate = base samplerate * relative play speed
        public float mBaseSamplerate;

        // Samplerate; samplerate = base samplerate * relative play speed
        public float mSamplerate;

        // Number of channels this audio source produces
        public uint mChannels;

        // Relative play speed; samplerate = base samplerate * relative play speed
        public float mSetRelativePlaySpeed;

        // Overall relative plays peed; overall = set * 3d
        public float mOverallRelativePlaySpeed;

        // How long this stream has played, in seconds.
        public Time mStreamTime;

        // Position of this stream, in samples.
        public ulong mStreamPosition;

        // Fader for the audio panning
        public Fader mPanFader;

        // Fader for the audio volume
        public Fader mVolumeFader;

        // Fader for the relative play speed
        public Fader mRelativePlaySpeedFader;

        // Fader used to schedule pausing of the stream
        public Fader mPauseScheduler;

        // Fader used to schedule stopping of the stream
        public Fader mStopScheduler;

        // Affected by some fader
        public int mActiveFader;

        // Current channel volumes, used to ramp the volume changes to avoid clicks
        public ChannelBuffer mCurrentChannelVolume;

        // ID of the sound source that generated this instance
        public uint mAudioSourceID;

        // Handle of the bus this audio instance is playing on. 0 for root.
        public Handle mBusHandle;

        // Filter pointer
        public FilterInstance?[] mFilter = new FilterInstance[SoLoud.FILTERS_PER_STREAM];

        // Initialize instance. Mostly internal use.
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

        // Pointers to buffers for the resampler
        public AlignedFloatBuffer mResampleData0;
        public AlignedFloatBuffer mResampleData1;

        // Sub-sample playhead; 16.16 fixed point
        public uint mSrcOffset;

        // Samples left over from earlier pass
        public uint mLeftoverSamples;

        // Number of samples to delay streaming
        public uint mDelaySamples;

        // When looping, start playing from this time
        public ulong mLoopPoint;

        // Get N samples from the stream to the buffer. Report samples written.
        public abstract uint getAudio(float* aBuffer, uint aSamplesToRead, uint aBufferSize);

        // Has the stream ended?
        public abstract bool hasEnded();

        // Seek to certain place in the stream. Base implementation is generic "tape" seek (and slow).
        public abstract SOLOUD_ERRORS seek(ulong aSamplePosition, float* mScratch, uint mScratchSize);

        // Get information. Returns 0 by default.
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
