using System.Numerics;
using LoudPizza.Core;
using LoudPizza.Modifiers;

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

        /// <inheritdoc cref="SoLoud.setMaxActiveVoiceCount(int)"/>
        public void SetMaxActiveVoiceCount(int voiceCount)
        {
            SoLoud.setMaxActiveVoiceCount(voiceCount); 
        }

        /// <inheritdoc cref="SoLoud.setPauseAll(bool)"/>
        public void SetPauseAll(bool pause)
        {
            SoLoud.setPauseAll(pause);
        }

        /// <inheritdoc/>
        public void SetVisualizationEnabled(bool enable)
        {
            SoLoud.SetVisualizationEnabled(enable);
        }

        /// <inheritdoc/>
        public bool GetVisualizationEnabled()
        {
            return SoLoud.GetVisualizationEnabled();
        }

        /// <inheritdoc cref="SoLoud.SetClipRoundoff(bool)"/>
        public void SetClipRoundoff(bool enable)
        {
            SoLoud.SetClipRoundoff(enable);
        }

        /// <inheritdoc cref="SoLoud.GetClipRoundoff"/>
        public bool GetClipRoundoff()
        {
            return SoLoud.GetClipRoundoff();
        }

        /// <inheritdoc cref="SoLoud.SetLeftHanded3D(bool)"/>
        public void SetLeftHanded3D(bool enable)
        {
            SoLoud.SetLeftHanded3D(enable);
        }

        /// <inheritdoc cref="SoLoud.GetLeftHanded3D"/>
        public bool GetLeftHanded3D()
        {
            return SoLoud.GetLeftHanded3D();
        }

        /// <inheritdoc cref="SoLoud.setSpeakerPosition(uint, Vector3)"/>
        public void SetSpeakerPosition(uint channel, Vector3 position)
        {
            SoLoud.setSpeakerPosition(channel, position);
        }
    }
}
