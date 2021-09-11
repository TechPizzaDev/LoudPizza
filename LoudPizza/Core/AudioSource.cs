using System;

namespace LoudPizza
{
    // Base class for audio sources
    public abstract class AudioSource : IDisposable
    {
        public enum FLAGS
        {
            // The instances from this audio source should loop
            SHOULD_LOOP = 1,
            // Only one instance of this audio source should play at the same time
            SINGLE_INSTANCE = 2,
            // Visualization data gathering enabled. Only for busses.
            VISUALIZATION_DATA = 4,
            // Audio instances created from this source are affected by 3d processing
            PROCESS_3D = 8,
            // Audio instances created from this source have listener-relative 3d coordinates
            LISTENER_RELATIVE = 16,
            // Delay start of sound by the distance from listener
            DISTANCE_DELAY = 32,
            // If inaudible, should be killed (default)
            INAUDIBLE_KILL = 64,
            // If inaudible, should still be ticked (default = pause)
            INAUDIBLE_TICK = 128,
            // Disable auto-stop
            DISABLE_AUTOSTOP = 256
        }

        private bool _isDisposed;

        // Flags. See AudioSource::FLAGS
        public FLAGS mFlags;

        // Base sample rate, used to initialize instances
        public float mBaseSamplerate;

        // Default volume for created instances
        public float mVolume;

        // Number of channels this audio source produces
        public uint mChannels;

        // Sound source ID. Assigned by SoLoud the first time it's played.
        public uint mAudioSourceID;

        // 3d min distance
        public float m3dMinDistance;

        // 3d max distance
        public float m3dMaxDistance;

        // 3d attenuation rolloff factor
        public float m3dAttenuationRolloff;

        // 3d doppler factor
        public float m3dDopplerFactor;

        // Filter pointer
        public Filter?[] mFilter = new Filter?[SoLoud.FILTERS_PER_STREAM];

        // Pointer to the Soloud object. Needed to stop all instances in dtor.
        public SoLoud mSoloud;

        // Pointer to a custom audio collider object
        public AudioCollider? mCollider;

        // Pointer to custom attenuator object
        public AudioAttenuator? mAttenuator;

        // User data related to audio collider
        public int mColliderData;

        // When looping, start playing from this time
        public ulong mLoopPoint;

        // CTor
        public AudioSource()
        {
            int i;
            for (i = 0; i < SoLoud.FILTERS_PER_STREAM; i++)
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

        // Set default volume for instances
        public void setVolume(float aVolume)
        {
            mVolume = aVolume;
        }

        // Set the looping of the instances created from this audio source
        public void setLooping(bool aLoop)
        {
            if (aLoop)
            {
                mFlags |= FLAGS.SHOULD_LOOP;
            }
            else
            {
                mFlags &= ~FLAGS.SHOULD_LOOP;
            }
        }

        // Set whether only one instance of this sound should ever be playing at the same time
        public void setSingleInstance(bool aSingleInstance)
        {
            if (aSingleInstance)
            {
                mFlags |= FLAGS.SINGLE_INSTANCE;
            }
            else
            {
                mFlags &= ~FLAGS.SINGLE_INSTANCE;
            }
        }

        // Set whether audio should auto-stop when it ends or not
        public void setAutoStop(bool aAutoStop)
        {
            if (aAutoStop)
            {
                mFlags &= ~FLAGS.DISABLE_AUTOSTOP;
            }
            else
            {
                mFlags |= FLAGS.DISABLE_AUTOSTOP;
            }
        }

        // Set the minimum and maximum distances for 3d audio source (closer to min distance = max vol)
        public void set3dMinMaxDistance(float aMinDistance, float aMaxDistance)
        {
            m3dMinDistance = aMinDistance;
            m3dMaxDistance = aMaxDistance;
        }

        // Set attenuation rolloff factor for 3d audio source
        public void set3dAttenuationRolloffFactor(float aAttenuationRolloffFactor)
        {
            m3dAttenuationRolloff = aAttenuationRolloffFactor;
        }

        // Set doppler factor to reduce or enhance doppler effect, default = 1.0
        public void set3dDopplerFactor(float aDopplerFactor)
        {
            m3dDopplerFactor = aDopplerFactor;
        }

        // Set the coordinates for this audio source to be relative to listener's coordinates.
        public void set3dListenerRelative(bool aListenerRelative)
        {
            if (aListenerRelative)
            {
                mFlags |= FLAGS.LISTENER_RELATIVE;
            }
            else
            {
                mFlags &= ~FLAGS.LISTENER_RELATIVE;
            }
        }

        // Enable delaying the start of the sound based on the distance.
        public void set3dDistanceDelay(bool aDistanceDelay)
        {
            if (aDistanceDelay)
            {
                mFlags |= FLAGS.DISTANCE_DELAY;
            }
            else
            {
                mFlags &= ~FLAGS.DISTANCE_DELAY;
            }
        }

        // Set a custom 3d audio collider. Set to NULL to disable.
        public void set3dCollider(AudioCollider? aCollider, int aUserData = 0)
        {
            mCollider = aCollider;
            mColliderData = aUserData;
        }

        // Set a custom attenuator. Set to NULL to disable.
        public void set3dAttenuator(AudioAttenuator? aAttenuator)
        {
            mAttenuator = aAttenuator;
        }

        // Set behavior for inaudible sounds
        public void setInaudibleBehavior(bool aMustTick, bool aKill)
        {
            mFlags &= ~(FLAGS.INAUDIBLE_KILL | FLAGS.INAUDIBLE_TICK);
            if (aMustTick)
            {
                mFlags |= FLAGS.INAUDIBLE_TICK;
            }
            if (aKill)
            {
                mFlags |= FLAGS.INAUDIBLE_KILL;
            }
        }

        // Set time to jump to when looping
        public void setLoopPoint(ulong aLoopPoint)
        {
            mLoopPoint = aLoopPoint;
        }

        // Get current loop point value
        public ulong getLoopPoint()
        {
            return mLoopPoint;
        }

        // Set filter. Set to NULL to clear the filter.
        public virtual void setFilter(uint aFilterId, Filter? aFilter)
        {
            if (aFilterId >= SoLoud.FILTERS_PER_STREAM)
                return;
            mFilter[aFilterId] = aFilter;
        }

        // Create instance from the audio source. Called from within Soloud class.
        public abstract AudioSourceInstance createInstance();

        // Stop all instances of this audio source
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
