using System;
using System.Numerics;
using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct VoiceHandle
    {
        /// <inheritdoc cref="SoLoud.set3dSourceParameters(Handle, Vector3, Vector3)"/>
        public void Set3DParameters(Vector3 position, Vector3 velocity)
        {
            SoLoud.set3dSourceParameters(Handle, position, velocity);
        }

        /// <inheritdoc cref="SoLoud.set3dSourcePosition(Handle, Vector3)"/>
        public void SetPosition(Vector3 position)
        {
            SoLoud.set3dSourcePosition(Handle, position);
        }

        /// <inheritdoc cref="SoLoud.set3dSourceVelocity(Handle, Vector3)"/>
        public void SetVelocity(Vector3 velocity)
        {
            SoLoud.set3dSourceVelocity(Handle, velocity);
        }

        /// <inheritdoc cref="SoLoud.set3dSourceMinMaxDistance(Handle, float, float)"/>
        public void SetMinMaxDistance(float minDistance, float maxDistance)
        {
            SoLoud.set3dSourceMinMaxDistance(Handle, minDistance, maxDistance);
        }

        /// <inheritdoc cref="SoLoud.set3dSourceCollider(Handle, AudioCollider?, IntPtr)"/>
        public void SetCollider(AudioCollider? collider, IntPtr userData = default)
        {
            SoLoud.set3dSourceCollider(Handle, collider, userData);
        }

        /// <inheritdoc cref="SoLoud.set3dSourceAttenuationRolloffFactor(Handle, float)"/>
        public void SetAttenuationRolloffFactor(float attenuationRolloffFactor)
        {
            SoLoud.set3dSourceAttenuationRolloffFactor(Handle, attenuationRolloffFactor);
        }

        /// <inheritdoc cref="SoLoud.set3dSourceAttenuator(Handle, AudioAttenuator?)"/>
        public void SetAttenuator(AudioAttenuator? attenuator)
        {
            SoLoud.set3dSourceAttenuator(Handle, attenuator);
        }

        /// <inheritdoc cref="SoLoud.set3dSourceDopplerFactor(Handle, float)"/>
        public void SetDopplerFactor(float dopplerFactor)
        {
            SoLoud.set3dSourceDopplerFactor(Handle, dopplerFactor);
        }
    }
}
