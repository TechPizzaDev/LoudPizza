using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct VoiceHandle
    {
        /// <inheritdoc cref="GetOverallVolume"/>
        public float OverallVolume => GetOverallVolume();

        /// <inheritdoc cref="GetStreamTime"/>
        public Time StreamTime => GetStreamTime();

        /// <inheritdoc cref="GetStreamTimePosition"/>
        public Time StreamTimePosition => GetStreamTimePosition();

        /// <inheritdoc cref="GetLoopCount"/>
        public ulong LoopCount => GetLoopCount();

        /// <inheritdoc cref="SoLoud.isValidVoiceHandle(Handle)"/>
        public bool IsValidVoiceHandle => SoLoud.isValidVoiceHandle(Handle);

        /// <inheritdoc cref="SoLoud.getLoopPoint(Handle)"/>
        public ulong GetLoopPoint()
        {
            return SoLoud.getLoopPoint(Handle);
        }

        /// <inheritdoc cref="SoLoud.getLooping(Handle)"/>
        public bool GetLooping()
        {
            return SoLoud.getLooping(Handle);
        }

        /// <inheritdoc cref="SoLoud.getAutoStop(Handle)"/>
        public bool GetAutoStop()
        {
            return SoLoud.getAutoStop(Handle);
        }

        /// <inheritdoc cref="SoLoud.getInfo(Handle, uint)"/>
        public float GetInfo(uint infoKey)
        {
            return SoLoud.getInfo(Handle, infoKey);
        }

        /// <inheritdoc cref="SoLoud.getVolume(Handle)"/>
        public float GetVolume()
        {
            return SoLoud.getVolume(Handle);
        }

        /// <inheritdoc cref="SoLoud.getOverallVolume(Handle)"/>
        public float GetOverallVolume()
        {
            return SoLoud.getOverallVolume(Handle);
        }

        /// <inheritdoc cref="SoLoud.getPan(Handle)"/>
        public float GetPan()
        {
            return SoLoud.getPan(Handle);
        }

        /// <inheritdoc cref="SoLoud.getStreamTime(Handle)"/>
        public Time GetStreamTime()
        {
            return SoLoud.getStreamTime(Handle);
        }

        /// <inheritdoc cref="SoLoud.getStreamSamplePosition(Handle)"/>
        public ulong GetStreamSamplePosition()
        {
            return SoLoud.getStreamSamplePosition(Handle);
        }

        /// <inheritdoc cref="SoLoud.getStreamTimePosition(Handle)"/>
        public Time GetStreamTimePosition()
        {
            return SoLoud.getStreamTimePosition(Handle);
        }

        /// <inheritdoc cref="SoLoud.getRelativePlaySpeed(Handle)"/>
        public float GetRelativePlaySpeed()
        {
            return SoLoud.getRelativePlaySpeed(Handle);
        }

        /// <inheritdoc cref="SoLoud.getSamplerate(Handle)"/>
        public float GetSampleRate()
        {
            return SoLoud.getSamplerate(Handle);
        }

        /// <inheritdoc cref="SoLoud.getPause(Handle)"/>
        public bool GetPause()
        {
            return SoLoud.getPause(Handle);
        }

        /// <inheritdoc cref="SoLoud.getProtectVoice(Handle)"/>
        public bool GetProtectVoice()
        {
            return SoLoud.getProtectVoice(Handle);
        }

        /// <inheritdoc cref="SoLoud.getLoopCount(Handle)"/>
        public uint GetLoopCount()
        {
            return SoLoud.getLoopCount(Handle);
        }
    }
}
