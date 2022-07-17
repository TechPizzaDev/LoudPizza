using System.Numerics;
using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct SoLoudHandle
    {
        /// <inheritdoc cref="SoLoud.setPostClipScaler(float)"/>
        public void SetPostClipScaler(float scaler)
        {
            SoLoud.setPostClipScaler(scaler);
        }

        /// <inheritdoc/>
        public void SetResampler(AudioResampler resampler)
        {
            SoLoud.SetResampler(resampler);
        }

        /// <inheritdoc cref="SoLoud.setGlobalVolume(float)"/>
        public void SetGlobalVolume(float volume)
        {
            SoLoud.setGlobalVolume(volume);
        }

        /// <inheritdoc cref="SoLoud.setMaxActiveVoiceCount(uint)"/>
        public void SetMaxActiveVoiceCount(uint voiceCount)
        {
            SoLoud.setMaxActiveVoiceCount(voiceCount); 
        }

        /// <inheritdoc cref="SoLoud.setPauseAll(bool)"/>
        public void SetPauseAll(bool pause)
        {
            SoLoud.setPauseAll(pause);
        }

        /// <inheritdoc/>
        public void SetVisualizationEnable(bool enable)
        {
            SoLoud.SetVisualizationEnable(enable);
        }

        /// <inheritdoc/>
        public bool GetVisualizationEnable()
        {
            return SoLoud.GetVisualizationEnable();
        }

        /// <inheritdoc cref="SoLoud.setSpeakerPosition(uint, Vector3)"/>
        public void SetSpeakerPosition(uint channel, Vector3 position)
        {
            SoLoud.setSpeakerPosition(channel, position);
        }
    }
}
