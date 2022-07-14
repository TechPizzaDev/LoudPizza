using System;

namespace LoudPizza
{
    /// <summary>
    /// Base class for audio sources. 
    /// </summary>
    public abstract class AudioSource : IDisposable
    {
        public enum Flags
        {
            /// <summary>
            /// The instances from this audio source should loop.
            /// </summary>
            ShouldLoop = 1,

            /// <summary>
            /// Only one instance of this audio source should play at the same time.
            /// </summary>
            SingleInstance = 2,

            /// <summary>
            /// Visualization data gathering enabled. Only for busses.
            /// </summary>
            VisualizationData = 4,

            /// <summary>
            /// Audio instances created from this source are affected by 3D processing.
            /// </summary>
            Process3D = 8,

            /// <summary>
            /// Audio instances created from this source have listener-relative 3D coordinates.
            /// </summary>
            ListenerRelative = 16,

            /// <summary>
            /// Delay start of sound by the distance from listener.
            /// </summary>
            DistanceDelay = 32,

            /// <summary>
            /// If inaudible, should be killed (default).
            /// </summary>
            InaudibleKill = 64,

            /// <summary>
            /// If inaudible, should still be ticked (default = pause).
            /// </summary>
            InaudibleTick = 128,

            /// <summary>
            /// Disable auto-stop.
            /// </summary>
            DisableAutostop = 256
        }

        private bool _isDisposed;

        public Flags mFlags;

        /// <summary>
        /// Base sample rate, used to initialize instances.
        /// </summary>
        public float mBaseSamplerate;

        /// <summary>
        /// Default volume for created instances.
        /// </summary>
        public float mVolume;

        /// <summary>
        /// Number of channels this audio source produces.
        /// </summary>
        public uint mChannels;

        /// <summary>
        /// Sound source ID. Assigned by SoLoud the first time it's played.
        /// </summary>
        public uint mAudioSourceID;

        /// <summary>
        /// 3D min distance.
        /// </summary>
        public float m3dMinDistance;

        /// <summary>
        /// 3D max distance.
        /// </summary>
        public float m3dMaxDistance;

        /// <summary>
        /// 3D attenuation rolloff factor.
        /// </summary>
        public float m3dAttenuationRolloff;

        /// <summary>
        /// 3D doppler factor.
        /// </summary>
        public float m3dDopplerFactor;

        /// <summary>
        /// Filters.
        /// </summary>
        public Filter?[] mFilter = new Filter?[SoLoud.FiltersPerStream];

        /// <summary>
        /// Pointer to the <see cref="SoLoud"/> object. Needed to stop all instances on <see cref="Dispose"/>.
        /// </summary>
        public SoLoud mSoloud;

        /// <summary>
        /// Custom audio collider object.
        /// </summary>
        public AudioCollider? mCollider;

        /// <summary>
        /// Custom attenuator object.
        /// </summary>
        public AudioAttenuator? mAttenuator;

        /// <summary>
        /// User data related to audio collider.
        /// </summary>
        public int mColliderData;

        /// <summary>
        /// When looping, start playing from this time.
        /// </summary>
        internal ulong mLoopPoint;

        public AudioSource()
        {
            int i;
            for (i = 0; i < SoLoud.FiltersPerStream; i++)
            {
                mFilter[i] = null;
            }
            mFlags = 0;
            mBaseSamplerate = 44100;
            mAudioSourceID = 0;
            mSoloud = null!;
            mChannels = 1;
            m3dMinDistance = 1;
            m3dMaxDistance = 1000000.0f;
            m3dAttenuationRolloff = 1.0f;
            m3dDopplerFactor = 1.0f;
            mCollider = null;
            mAttenuator = null;
            mColliderData = 0;
            mVolume = 1;
            mLoopPoint = 0;
        }

        /// <summary>
        /// Set default volume for instances.
        /// </summary>
        public void setVolume(float aVolume)
        {
            mVolume = aVolume;
        }

        /// <summary>
        /// Set the looping of the instances created from this audio source.
        /// </summary>
        public void setLooping(bool aLoop)
        {
            if (aLoop)
            {
                mFlags |= Flags.ShouldLoop;
            }
            else
            {
                mFlags &= ~Flags.ShouldLoop;
            }
        }

        /// <summary>
        /// Set whether only one instance of this sound should ever be playing at the same time.
        /// </summary>
        public void setSingleInstance(bool aSingleInstance)
        {
            if (aSingleInstance)
            {
                mFlags |= Flags.SingleInstance;
            }
            else
            {
                mFlags &= ~Flags.SingleInstance;
            }
        }

        /// <summary>
        /// Set whether audio should auto-stop when it ends or not.
        /// </summary>
        /// <param name="aAutoStop"></param>
        public void setAutoStop(bool aAutoStop)
        {
            if (aAutoStop)
            {
                mFlags &= ~Flags.DisableAutostop;
            }
            else
            {
                mFlags |= Flags.DisableAutostop;
            }
        }

        /// <summary>
        /// Set the minimum and maximum distances for 3D audio source (closer to min distance = max volume).
        /// </summary>
        public void set3dMinMaxDistance(float aMinDistance, float aMaxDistance)
        {
            m3dMinDistance = aMinDistance;
            m3dMaxDistance = aMaxDistance;
        }

        /// <summary>
        /// Set attenuation rolloff factor for 3D audio source.
        /// </summary>
        public void set3dAttenuationRolloffFactor(float aAttenuationRolloffFactor)
        {
            m3dAttenuationRolloff = aAttenuationRolloffFactor;
        }

        /// <summary>
        /// Set doppler factor to reduce or enhance doppler effect (default = 1.0).
        /// </summary>
        public void set3dDopplerFactor(float aDopplerFactor)
        {
            m3dDopplerFactor = aDopplerFactor;
        }

        /// <summary>
        /// Set the coordinates for this audio source to be relative to listener's coordinates.
        /// </summary>
        public void set3dListenerRelative(bool aListenerRelative)
        {
            if (aListenerRelative)
            {
                mFlags |= Flags.ListenerRelative;
            }
            else
            {
                mFlags &= ~Flags.ListenerRelative;
            }
        }

        /// <summary>
        /// Enable delaying the start of the sound based on the distance.
        /// </summary>
        public void set3dDistanceDelay(bool aDistanceDelay)
        {
            if (aDistanceDelay)
            {
                mFlags |= Flags.DistanceDelay;
            }
            else
            {
                mFlags &= ~Flags.DistanceDelay;
            }
        }

        /// <summary>
        /// Set a custom 3D audio collider. Set to <see langword="null"/> to disable.
        /// </summary>
        public void set3dCollider(AudioCollider? aCollider, int aUserData = 0)
        {
            mCollider = aCollider;
            mColliderData = aUserData;
        }

        /// <summary>
        /// Set a custom attenuator. Set to <see langword="null"/> to disable.
        /// </summary>
        public void set3dAttenuator(AudioAttenuator? aAttenuator)
        {
            mAttenuator = aAttenuator;
        }

        /// <summary>
        /// Set behavior for inaudible sounds.
        /// </summary>
        public void setInaudibleBehavior(bool aMustTick, bool aKill)
        {
            mFlags &= ~(Flags.InaudibleKill | Flags.InaudibleTick);
            if (aMustTick)
            {
                mFlags |= Flags.InaudibleTick;
            }
            if (aKill)
            {
                mFlags |= Flags.InaudibleKill;
            }
        }

        /// <summary>
        /// Set time to jump to when looping.
        /// </summary>
        public void setLoopPoint(ulong aLoopPoint)
        {
            mLoopPoint = aLoopPoint;
        }

        /// <summary>
        /// Get current loop point value.
        /// </summary>
        public ulong getLoopPoint()
        {
            return mLoopPoint;
        }

        /// <summary>
        /// Set filter. Set to <see langword="null"/> to clear the filter.
        /// </summary>
        public virtual void setFilter(uint aFilterId, Filter? aFilter)
        {
            if (aFilterId >= SoLoud.FiltersPerStream)
                return;
            mFilter[aFilterId] = aFilter;
        }

        /// <summary>
        /// Create instance from the audio source. Called from within <see cref="SoLoud"/> class.
        /// </summary>
        public abstract AudioSourceInstance createInstance();

        /// <summary>
        /// Stop all instances of this audio source.
        /// </summary>
        public void stop()
        {
            if (mSoloud != null)
            {
                mSoloud.stopAudioSource(this);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                stop();
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~AudioSource()
        {
            Dispose(disposing: false);
        }
    }
}
