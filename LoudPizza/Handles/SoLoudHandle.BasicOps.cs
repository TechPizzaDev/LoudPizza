using LoudPizza.Core;
using LoudPizza.Sources;

namespace LoudPizza
{
    public readonly partial struct SoLoudHandle
    {
        /// <inheritdoc cref="IAudioBus.Play"/>
        public VoiceHandle Play(AudioSource source, float volume = -1.0f, float pan = 0.0f, bool paused = false, Handle bus = default)
        {
            Handle handle = SoLoud.play(source, volume, pan, paused, bus);
            return new VoiceHandle(SoLoud, handle);
        }

        VoiceHandle IAudioBus.Play(AudioSource source, float volume, float pan, bool paused)
        {
            return Play(source, volume, pan, paused, default);
        }

        /// <inheritdoc cref="IAudioBus.PlayClocked"/>
        public VoiceHandle PlayClocked(AudioSource source, Time soundTime, float volume = -1.0f, float pan = 0.0f, Handle bus = default)
        {
            Handle handle = SoLoud.playClocked(soundTime, source, volume, pan, bus);
            return new VoiceHandle(SoLoud, handle);
        }

        VoiceHandle IAudioBus.PlayClocked(AudioSource source, Time soundTime, float volume, float pan)
        {
            return PlayClocked(source, soundTime, volume, pan, default);
        }

        /// <inheritdoc cref="IAudioBus.PlayBackground"/>
        public VoiceHandle PlayBackground(AudioSource source, float volume = 1.0f, bool paused = false, Handle bus = default)
        {
            Handle handle = SoLoud.playBackground(source, volume, paused, bus);
            return new VoiceHandle(SoLoud, handle);
        }

        VoiceHandle IAudioBus.PlayBackground(AudioSource source, float volume, bool paused)
        {
            return PlayBackground(source, volume, paused, default);
        }

        /// <inheritdoc cref="SoLoud.stopAudioSource(AudioSource)"/>
        public void StopAudioSource(AudioSource source)
        {
            SoLoud.stopAudioSource(source);
        }

        /// <inheritdoc cref="SoLoud.stopAll"/>
        public void StopAll()
        {
            SoLoud.stopAll();
        }

        /// <inheritdoc cref="SoLoud.countAudioSource"/>
        public int CountAudioSource(AudioSource source)
        {
            return SoLoud.countAudioSource(source);
        }
    }
}
