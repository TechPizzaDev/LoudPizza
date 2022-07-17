using System;
using System.Numerics;

namespace LoudPizza.Core
{
    public struct AudioSourceInstance3dData
    {
        /// <summary>
        /// Set settings from an <see cref="AudioSource"/>.
        /// </summary>
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
            mChannelVolume = default;
        }

        /// <summary>
        /// 3D position.
        /// </summary>
        public Vector3 m3dPosition;

        /// <summary>
        /// 3D velocity.
        /// </summary>
        public Vector3 m3dVelocity;

        // 3D cone direction
        /*
        float m3dConeDirection[3];
        // 3D cone inner angle
        float m3dConeInnerAngle;
        // 3D cone outer angle
        float m3dConeOuterAngle;
        // 3D cone outer volume multiplier
        float m3dConeOuterVolume;
        */

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
        /// Custom audio collider object.
        /// </summary>
        public AudioCollider? mCollider;

        /// <summary>
        /// Custom audio attenuator object.
        /// </summary>
        public AudioAttenuator? mAttenuator;

        /// <summary>
        /// User data related to audio collider.
        /// </summary>
        public IntPtr mColliderData;

        /// <summary>
        /// Doppler sample rate multiplier.
        /// </summary>
        public float mDopplerValue;

        /// <summary>
        /// Overall 3D volume.
        /// </summary>
        public float m3dVolume;

        /// <summary>
        /// Channel volume.
        /// </summary>
        public ChannelBuffer mChannelVolume;

        /// <summary>
        /// Copy of flags.
        /// </summary>
        public AudioSourceInstance.Flags mFlags;

        /// <summary>
        /// Latest handle for this voice.
        /// </summary>
        public Handle mHandle;
    };
}
