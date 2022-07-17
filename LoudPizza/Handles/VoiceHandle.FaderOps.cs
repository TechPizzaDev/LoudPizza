using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct VoiceHandle
    {
        /// <inheritdoc cref="SoLoud.schedulePause(Handle, Time)"/>
        public void SchedulePause(Time time)
        {
            SoLoud.schedulePause(Handle, time);
        }

        /// <inheritdoc cref="SoLoud.scheduleStop(Handle, Time)"/>
        public void ScheduleStop(Time time)
        {
            SoLoud.scheduleStop(Handle, time);
        }

        /// <inheritdoc cref="SoLoud.fadeVolume(Handle, float, Time)"/>
        public void FadeVolume(float to, Time time)
        {
            SoLoud.fadeVolume(Handle, to, time);
        }

        /// <inheritdoc cref="SoLoud.fadePan(Handle, float, Time)"/>
        public void FadePan(float to, Time time)
        {
            SoLoud.fadePan(Handle, to, time);
        }

        /// <inheritdoc cref="SoLoud.fadeRelativePlaySpeed(Handle, float, Time)"/>
        public void FadeRelativePlaySpeed(float to, Time time)
        {
            SoLoud.fadeRelativePlaySpeed(Handle, to, time);
        }

        /// <inheritdoc cref="SoLoud.oscillateVolume(Handle, float, float, Time)"/>
        public void OscillateVolume(float from, float to, Time time)
        {
            SoLoud.oscillateVolume(Handle, from, to, time);
        }

        /// <inheritdoc cref="SoLoud.oscillatePan(Handle, float, float, Time)"/>
        public void OscillatePan(float from, float to, Time time)
        {
            SoLoud.oscillatePan(Handle, from, to, time);
        }

        /// <inheritdoc cref="SoLoud.oscillateRelativePlaySpeed(Handle, float, float, Time)"/>
        public void OscillateRelativePlaySpeed(float from, float to, Time time)
        {
            SoLoud.oscillateRelativePlaySpeed(Handle, from, to, time);
        }
    }
}
