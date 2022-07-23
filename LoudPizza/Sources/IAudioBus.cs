using System.Numerics;
using LoudPizza.Core;
using LoudPizza.Modifiers;

namespace LoudPizza.Sources
{
    public interface IAudioBus
    {
        /// <summary>
        /// Start playing a sound from an audio source.
        /// </summary>
        /// <param name="volume">Negative volume means to use the default from <paramref name="source"/>.</param>
        /// <returns>The voice handle, which can be ignored or used to alter the playing sound's parameters. </returns>
        VoiceHandle Play(AudioSource source, float volume = -1.0f, float pan = 0.0f, bool paused = false);

        /// <summary>
        /// Start playing a sound from an audio source, delayed in relation to other sounds called via this function. 
        /// </summary>
        /// <param name="volume">Negative volume means to use the default from <paramref name="source"/>.</param>
        /// <returns>The voice handle, which can be ignored or used to alter the playing sound's parameters. </returns>
        VoiceHandle PlayClocked(AudioSource source, Time soundTime, float volume = -1.0f, float pan = 0.0f);

        /// <summary>
        /// Start playing a sound without any panning.
        /// </summary>
        /// <remarks>
        /// It will be played at full volume.
        /// </remarks>
        /// <param name="volume">Negative volume means to use the default from <paramref name="source"/>.</param>
        /// <returns>The voice handle, which can be ignored or used to alter the playing sound's parameters. </returns>
        VoiceHandle PlayBackground(AudioSource source, float volume = 1.0f, bool paused = false);

        /// <summary>
        /// Start playing a 3D sound from an audio source.
        /// </summary>
        /// <param name="volume">Negative volume means to use the default from <paramref name="source"/>.</param>
        /// <returns>The voice handle, which can be ignored or used to alter the playing sound's parameters. </returns>
        VoiceHandle Play3D(
            AudioSource source,
            Vector3 position,
            Vector3 velocity = default,
            float volume = -1.0f,
            bool paused = false);

        /// <summary>
        /// Start playing a 3D sound from an audio source, delayed in relation to other sounds called via this function.
        /// </summary>
        /// <param name="volume">Negative volume means to use the default from <paramref name="source"/>.</param>
        /// <returns>The voice handle, which can be ignored or used to alter the playing sound's parameters. </returns>
        VoiceHandle PlayClocked3D(
            AudioSource source,
            Time soundTime,
            Vector3 position,
            Vector3 velocity = default,
            float volume = -1.0f);

        /// <summary>
        /// Enable or disable visualization data gathering.
        /// </summary>
        public void SetVisualizationEnabled(bool enable);

        /// <summary>
        /// Get whether visualization data gathering is enabled.
        /// </summary>
        public bool GetVisualizationEnabled();

        /// <summary>
        /// Move a live sound to this bus.
        /// </summary>
        void AnnexSound(Handle voiceHandle);

        /// <summary>
        /// Calculate and get 256 floats of FFT data for visualization.
        /// </summary>
        /// <remarks>
        /// Visualization has to be enabled before use.
        /// </remarks>
        void CalcFFT(out Buffer256 data);

        /// <summary>
        /// Get 256 floats of wave data for visualization.
        /// </summary>
        /// <remarks>
        /// Visualization has to be enabled before use.
        /// </remarks>
        void GetWave(out Buffer256 data);

        /// <summary>
        /// Get approximate volume for output channel for visualization.
        /// </summary>
        /// <remarks>
        /// Visualization has to be enabled before use.
        /// </remarks>
        float GetApproximateVolume(uint channel);

        /// <summary>
        /// Get approximate volumes for all output channels for visualization.
        /// </summary>
        /// <remarks>
        /// Visualization has to be enabled before use.
        /// </remarks>
        void GetApproximateVolumes(out ChannelBuffer buffer);

        /// <summary>
        /// Get the current number of busy voices.
        /// </summary>
        int GetActiveVoiceCount();

        /// <summary>
        /// Get current the resampler for this bus.
        /// </summary>
        AudioResampler GetResampler();

        /// <summary>
        /// Set the resampler for this bus.
        /// </summary>
        void SetResampler(AudioResampler resampler);

        /// <summary>
        /// Get the handle for this playing bus.
        /// </summary>
        Handle GetBusHandle();
    }
}
