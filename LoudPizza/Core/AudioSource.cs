using System;

namespace LoudPizza.Core
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

        internal Flags mFlags;

        /// <summary>
        /// Base sample rate, used to initialize instances.
        /// </summary>
        internal float mBaseSamplerate;

        /// <summary>
        /// Default volume for created instances.
        /// </summary>
        internal float mVolume;

        /// <summary>
        /// Number of channels this audio source produces.
        /// </summary>
        internal uint mChannels;

        /// <summary>
        /// 3D min distance.
        /// </summary>
        internal float m3dMinDistance;

        /// <summary>
        /// 3D max distance.
        /// </summary>
        internal float m3dMaxDistance;

        /// <summary>
        /// 3D attenuation rolloff factor.
        /// </summary>
        internal float m3dAttenuationRolloff;

        /// <summary>
        /// 3D doppler factor.
        /// </summary>
        internal float m3dDopplerFactor;

        /// <summary>
        /// Filters.
        /// </summary>
        internal Filter?[] mFilter = new Filter?[SoLoud.FiltersPerStream];

        /// <summary>
        /// Pointer to the <see cref="Core.SoLoud"/> object. Needed to stop all instances on <see cref="Dispose"/>.
        /// </summary>
        public SoLoud SoLoud { get; }

        /// <summary>
        /// Custom audio collider object.
        /// </summary>
        internal AudioCollider? mCollider;

        /// <summary>
        /// Custom attenuator object.
        /// </summary>
        internal AudioAttenuator? mAttenuator;

        /// <summary>
        /// User data related to audio collider.
        /// </summary>
        internal IntPtr mColliderData;

        /// <summary>
        /// When looping, start playing from this time.
        /// </summary>
        internal ulong mLoopPoint;

        public AudioSource(SoLoud soLoud)
        {
            SoLoud = soLoud ?? throw new ArgumentNullException(nameof(soLoud));

            mFilter.AsSpan().Clear();
            mFlags = 0;
            mBaseSamplerate = 44100;
            mChannels = 1;
            m3dMinDistance = 1;
            m3dMaxDistance = 1000000.0f;
            m3dAttenuationRolloff = 1.0f;
            m3dDopplerFactor = 1.0f;
            mCollider = null;
            mAttenuator = null;
            mColliderData = default;
            mVolume = 1;
            mLoopPoint = 0;
        }

        /// <summary>
        /// Set default volume for instances.
        /// </summary>
        public void SetVolume(float volume)
        {
            mVolume = volume;
        }

        /// <summary>
        /// Get default volume for instances.
        /// </summary>
        public float GetVolume()
        {
            return mVolume;
        }

        /// <summary>
        /// Set the default looping value for instances.
        /// </summary>
        public void SetLooping(bool loop)
        {
            if (loop)
            {
                mFlags |= Flags.ShouldLoop;
            }
            else
            {
                mFlags &= ~Flags.ShouldLoop;
            }
        }

        /// <summary>
        /// Gets the default looping value for instances.
        /// </summary>
        public bool GetLooping()
        {
            return (mFlags & Flags.ShouldLoop) != 0;
        }

        /// <summary>
        /// Set whether only one instance of this sound should ever be playing at the same time.
        /// </summary>
        public void SetSingleInstance(bool singleInstance)
        {
            if (singleInstance)
            {
                mFlags |= Flags.SingleInstance;
            }
            else
            {
                mFlags &= ~Flags.SingleInstance;
            }
        }

        /// <summary>
        /// Get whether only one instance of this sound should ever be playing at the same time.
        /// </summary>
        public bool GetSingleInstance()
        {
            return (mFlags & Flags.SingleInstance) != 0;
        }

        /// <summary>
        /// Set whether audio should auto-stop when it ends or not.
        /// </summary>
        public void SetAutoStop(bool autoStop)
        {
            if (autoStop)
            {
                mFlags &= ~Flags.DisableAutostop;
            }
            else
            {
                mFlags |= Flags.DisableAutostop;
            }
        }

        /// <summary>
        /// Set whether audio should auto-stop when it ends or not.
        /// </summary>
        public bool GetAutoStop()
        {
            return (mFlags & Flags.DisableAutostop) == 0;
        }

        /// <summary>
        /// Set the minimum and maximum distances for 3D audio source (closer to min distance = max volume).
        /// </summary>
        public void SetMinMaxDistance(float minDistance, float maxDistance)
        {
            m3dMinDistance = minDistance;
            m3dMaxDistance = maxDistance;
        }

        /// <summary>
        /// Get the minimum and maximum distances for 3D audio source (closer to min distance = max volume).
        /// </summary>
        public void GetMinMaxDistance(out float minDistance, out float maxDistance)
        {
            minDistance = m3dMinDistance;
            maxDistance = m3dMaxDistance;
        }

        /// <summary>
        /// Set attenuation rolloff factor for 3D audio source.
        /// </summary>
        public void SetAttenuationRolloffFactor(float attenuationRolloffFactor)
        {
            m3dAttenuationRolloff = attenuationRolloffFactor;
        }

        /// <summary>
        /// Get attenuation rolloff factor for 3D audio source.
        /// </summary>
        public float GetAttenuationRolloffFactor()
        {
            return m3dAttenuationRolloff;
        }

        /// <summary>
        /// Set doppler factor to reduce or enhance doppler effect (default = 1.0).
        /// </summary>
        public void SetDopplerFactor(float dopplerFactor)
        {
            m3dDopplerFactor = dopplerFactor;
        }

        /// <summary>
        /// Get doppler factor to reduce or enhance doppler effect (default = 1.0).
        /// </summary>
        public float GetDopplerFactor()
        {
            return m3dDopplerFactor;
        }

        /// <summary>
        /// Set the coordinates for this audio source to be relative to listener's coordinates.
        /// </summary>
        public void SetListenerRelative(bool listenerRelative)
        {
            if (listenerRelative)
            {
                mFlags |= Flags.ListenerRelative;
            }
            else
            {
                mFlags &= ~Flags.ListenerRelative;
            }
        }

        /// <summary>
        /// Get the coordinates for this audio source to be relative to listener's coordinates.
        /// </summary>
        public bool GetListenerRelative()
        {
            return (mFlags & Flags.ListenerRelative) != 0;
        }

        /// <summary>
        /// Set whether delaying the start of the sound based on the distance is enabled.
        /// </summary>
        public void SetDistanceDelay(bool distanceDelay)
        {
            if (distanceDelay)
            {
                mFlags |= Flags.DistanceDelay;
            }
            else
            {
                mFlags &= ~Flags.DistanceDelay;
            }
        }

        /// <summary>
        /// Get whether delaying the start of the sound based on the distance is enabled.
        /// </summary>
        public bool SetDistanceDelay()
        {
            return (mFlags & Flags.DistanceDelay) != 0;
        }

        /// <summary>
        /// Set a custom 3D audio collider. Set to <see langword="null"/> to disable.
        /// </summary>
        public void SetCollider(AudioCollider? collider, IntPtr userData = default)
        {
            mCollider = collider;
            mColliderData = userData;
        }

        /// <summary>
        /// Get the custom 3D audio collider. Can be <see langword="null"/>.
        /// </summary>
        public AudioCollider? GetAudioCollider(out IntPtr userData)
        {
            userData = mColliderData;
            return mCollider;
        }

        /// <summary>
        /// Set a custom attenuator. Set to <see langword="null"/> to disable.
        /// </summary>
        public void SetAttenuator(AudioAttenuator? attenuator)
        {
            mAttenuator = attenuator;
        }

        /// <summary>
        /// Get the custom attenuator. Can be <see langword="null"/>.
        /// </summary>
        public AudioAttenuator? GetAttenuator()
        {
            return mAttenuator;
        }

        /// <summary>
        /// Set behavior for inaudible sounds.
        /// </summary>
        public void SetInaudibleBehavior(bool mustTick, bool kill)
        {
            mFlags &= ~(Flags.InaudibleKill | Flags.InaudibleTick);
            if (mustTick)
            {
                mFlags |= Flags.InaudibleTick;
            }
            if (kill)
            {
                mFlags |= Flags.InaudibleKill;
            }
        }

        /// <summary>
        /// Get behavior for inaudible sounds.
        /// </summary>
        public void GetInaudibleBehavior(out bool mustTick, out bool kill)
        {
            mustTick = (mFlags & Flags.InaudibleTick) != 0;
            kill = (mFlags & Flags.InaudibleKill) != 0;
        }

        /// <summary>
        /// Set time to jump to when looping.
        /// </summary>
        public void SetLoopPoint(ulong loopPoint)
        {
            mLoopPoint = loopPoint;
        }

        /// <summary>
        /// Get current loop point value.
        /// </summary>
        public ulong GetLoopPoint()
        {
            return mLoopPoint;
        }

        /// <summary>
        /// Set filter. Set to <see langword="null"/> to clear the filter.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public virtual void SetFilter(uint filterId, Filter? filter)
        {
            if (filterId >= SoLoud.FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(filterId));
            }
            mFilter[filterId] = filter;
        }

        /// <summary>
        /// Get filter. Can be <see langword="null"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterId"/> is invalid.</exception>
        public Filter? GetFilter(uint filterId)
        {
            if (filterId >= SoLoud.FiltersPerStream)
            {
                throw new ArgumentOutOfRangeException(nameof(filterId));
            }
            return mFilter[filterId];
        }

        /// <summary>
        /// Create instance from the audio source. Called from within <see cref="Core.SoLoud"/> class.
        /// </summary>
        public abstract AudioSourceInstance CreateInstance();

        /// <summary>
        /// Stop all instances of this audio source.
        /// </summary>
        public void Stop()
        {
            if (SoLoud != null)
            {
                SoLoud.stopAudioSource(this);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                Stop();
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
