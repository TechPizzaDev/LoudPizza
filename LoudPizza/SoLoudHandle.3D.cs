using System;
using System.Diagnostics;
using System.Numerics;
using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct SoLoudHandle
    {
        /// <inheritdoc cref="SoLoud.update3dAudio"/>
        public void UpdateAudio3D()
        {
            SoLoud.update3dAudio();
        }

        /// <inheritdoc cref="SoLoud.play3d(AudioSource, Vector3, Vector3, float, bool, Handle)"/>
        public VoiceHandle Play3D(
            AudioSource source,
            Vector3 position,
            Vector3 velocity = default,
            float volume = -1.0f,
            bool paused = false,
            Handle bus = default)
        {
            Handle handle = SoLoud.play3d(source, position, velocity, volume, paused, bus);
            return new VoiceHandle(SoLoud, handle);
        }

        VoiceHandle IAudioBus.Play3D(AudioSource source, Vector3 position, Vector3 velocity, float volume, bool paused)
        {
            return Play3D(source, position, velocity, volume, paused, default);
        }

        /// <inheritdoc cref="SoLoud.play3dClocked(Time, AudioSource, Vector3, Vector3, float, Handle)"/>
        public VoiceHandle PlayClocked3D(
            AudioSource source,
            Time soundTime,
            Vector3 position,
            Vector3 velocity = default,
            float volume = -1.0f,
            Handle bus = default)
        {
            Handle handle = SoLoud.play3dClocked(soundTime, source, position, velocity, volume, bus);
            return new VoiceHandle(SoLoud, handle);
        }

        VoiceHandle IAudioBus.PlayClocked3D(AudioSource source, Time soundTime, Vector3 position, Vector3 velocity, float volume)
        {
            return PlayClocked3D(source, soundTime, position, velocity, volume, default);
        }

        /// <inheritdoc cref="SoLoud.set3dSoundSpeed"/>
        /// <exception cref="ArgumentOutOfRangeException">The speed is less than or equal to zero.</exception>
        public void SetSoundSpeed(float aSpeed)
        {
            if (aSpeed <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(aSpeed));
            }

            SoLoudStatus status = SoLoud.set3dSoundSpeed(aSpeed);
            Debug.Assert(status == SoLoudStatus.Ok);
        }

        /// <inheritdoc cref="SoLoud.get3dSoundSpeed"/>
        public float GetSoundSpeed()
        {
            return SoLoud.get3dSoundSpeed();
        }

        /// <inheritdoc cref="SoLoud.set3dListenerParameters(Vector3, Vector3, Vector3, Vector3)"/>
        public void SetListenerParameters(
            Vector3 position,
            Vector3 at,
            Vector3 up,
            Vector3 velocity)
        {
            SoLoud.set3dListenerParameters(position, at, up, velocity);
        }

        /// <inheritdoc cref="SoLoud.set3dListenerPosition(Vector3)"/>
        public void SetListenerPosition(Vector3 position)
        {
            SoLoud.set3dListenerPosition(position);
        }

        /// <inheritdoc cref="SoLoud.set3dListenerAt(Vector3)"/>
        public void SetListenerAt(Vector3 at)
        {
            SoLoud.set3dListenerAt(at);
        }

        /// <inheritdoc cref="SoLoud.set3dListenerUp(Vector3)"/>
        public void SetListenerUp(Vector3 up)
        {
            SoLoud.set3dListenerUp(up);
        }

        /// <inheritdoc cref="SoLoud.set3dListenerVelocity(Vector3)"/>
        public void SetListenerVelocity(Vector3 velocity)
        {
            SoLoud.set3dListenerVelocity(velocity);
        }

    }
}
