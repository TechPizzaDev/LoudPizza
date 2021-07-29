
namespace LoudPizza
{
    public struct AudioSourceInstance3dData
    {
        // ctor
        public static AudioSourceInstance3dData ctor()
        {
            AudioSourceInstance3dData s;
            s.m3dAttenuationRolloff = 1;
            s.m3dDopplerFactor = 1.0f;
            s.m3dMaxDistance = 1000000.0f;
            s.m3dMinDistance = 0.0f;
            s.m3dVolume = 0;
            s.mCollider = null;
            s.mColliderData = 0;
            s.mAttenuator = null;
            s.mDopplerValue = 0;
            s.mFlags = 0;
            s.mHandle = default;
            s.m3dVelocity = default;
            s.m3dPosition = default;
            for (int i = 0; i < SoLoud.MAX_CHANNELS; i++)
                s.mChannelVolume[i] = 0;
            return s;
        }

        // Set settings from audiosource
        public void init(AudioSource aSource)
        {
            m3dAttenuationRolloff = aSource.m3dAttenuationRolloff;
            m3dDopplerFactor = aSource.m3dDopplerFactor;
            m3dMaxDistance = aSource.m3dMaxDistance;
            m3dMinDistance = aSource.m3dMinDistance;
            mCollider = aSource.mCollider;
            mColliderData = aSource.mColliderData;
            mAttenuator = aSource.mAttenuator;
            m3dVolume = 1.0f;
            mDopplerValue = 1.0f;

            mFlags = 0;
            mHandle = default;
            m3dVelocity = default;
            m3dPosition = default;
            for (int i = 0; i < SoLoud.MAX_CHANNELS; i++)
                mChannelVolume[i] = 0;
        }

        // 3d position
        public Vec3 m3dPosition;

        // 3d velocity
        public Vec3 m3dVelocity;

        // 3d cone direction
        /*
        float m3dConeDirection[3];
        // 3d cone inner angle
        float m3dConeInnerAngle;
        // 3d cone outer angle
        float m3dConeOuterAngle;
        // 3d cone outer volume multiplier
        float m3dConeOuterVolume;
        */
        // 3d min distance
        public float m3dMinDistance;

        // 3d max distance
        public float m3dMaxDistance;

        // 3d attenuation rolloff factor
        public float m3dAttenuationRolloff;

        // 3d doppler factor
        public float m3dDopplerFactor;

        // Pointer to a custom audio collider object
        public AudioCollider? mCollider;

        // Pointer to a custom audio attenuator object
        public AudioAttenuator? mAttenuator;

        // User data related to audio collider
        public int mColliderData;

        // Doppler sample rate multiplier
        public float mDopplerValue;

        // Overall 3d volume
        public float m3dVolume;

        // Channel volume
        public ChannelBuffer mChannelVolume;

        // Copy of flags
        public AudioSourceInstance.FLAGS mFlags;

        // Latest handle for this voice
        public Handle mHandle;
    };
}
