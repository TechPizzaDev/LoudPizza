using System;
using System.Diagnostics;
using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct VoiceHandle
    {
        /// <inheritdoc cref="SoLoud.setRelativePlaySpeed(Handle, float)"/>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="speed"/> is less than or equal to zero.</exception>
        public void SetRelativePlaySpeed(float speed)
        {
            if (!(speed > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(speed));
            }

            SoLoudStatus status = SoLoud.setRelativePlaySpeed(Handle, speed);
            Debug.Assert(status == SoLoudStatus.Ok);
        }

        /// <inheritdoc cref="SoLoud.setSamplerate(Handle, float)"/>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="speed"/> is less than zero.</exception>
        public void SetSampleRate(float sampleRate)
        {
            if (float.IsNegative(sampleRate))
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            SoLoud.setSamplerate(Handle, sampleRate);
        }

        /// <inheritdoc cref="SoLoud.setPause(Handle, bool)"/>
        public void SetPause(bool pause)
        {
            SoLoud.setPause(Handle, pause);
        }

        /// <inheritdoc cref="SoLoud.setProtectVoice(Handle, bool)"/>
        public void SetProtectVoice(bool protect)
        {
            SoLoud.setProtectVoice(Handle, protect);
        }

        /// <inheritdoc cref="SoLoud.setPan(Handle, float)"/>
        public void SetPan(float pan)
        {
            SoLoud.setPan(Handle, pan);
        }

        /// <inheritdoc cref="SoLoud.setChannelVolume(Handle, uint, float)"/>
        public void SetChannelVolume(uint channel, float volume)
        {
            SoLoud.setChannelVolume(Handle, channel, volume);
        }

        /// <inheritdoc cref="SoLoud.setPanAbsolute(Handle, float, float)"/>
        public void SetPanAbsolute(float leftVolume, float rightVolume)
        {
            SoLoud.setPanAbsolute(Handle, leftVolume, rightVolume);
        }

        /// <inheritdoc cref="SoLoud.setInaudibleBehavior(Handle, bool, bool)"/>
        public void SetInaudibleBehavior(bool mustTick, bool kill)
        {
            SoLoud.setInaudibleBehavior(Handle, mustTick, kill);
        }

        /// <inheritdoc cref="SoLoud.setLoopPoint(Handle, ulong)"/>
        public void SetLoopPoint(ulong loopPoint)
        {
            SoLoud.setLoopPoint(Handle, loopPoint);
        }

        /// <inheritdoc cref="SoLoud.setLooping(Handle, bool)"/>
        public void SetLooping(bool looping)
        {
            SoLoud.setLooping(Handle, looping);
        }

        /// <inheritdoc cref="SoLoud.setAutoStop(Handle, bool)"/>
        public void SetAutoStop(bool autoStop)
        {
            SoLoud.setAutoStop(Handle, autoStop);
        }

        /// <inheritdoc cref="SoLoud.setVolume(Handle, float)"/>
        public void SetVolume(float volume)
        {
            SoLoud.setVolume(Handle, volume);
        }

        /// <inheritdoc cref="SoLoud.setDelaySamples(Handle, uint)"/>
        public void SetDelaySamples(uint samples)
        {
            SoLoud.setDelaySamples(Handle, samples);
        }
    }
}
