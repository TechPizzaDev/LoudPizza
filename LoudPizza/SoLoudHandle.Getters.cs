using System;
using System.Numerics;
using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct SoLoudHandle
    {
        /// <inheritdoc cref="SoLoud.getVersion"/>
        public uint GetVersion()
        {
            return SoLoud.getVersion();
        }

        /// <inheritdoc cref="SoLoud.getPostClipScaler"/>
        public float GetPostClipScaler()
        {
            return SoLoud.getPostClipScaler();
        }

        /// <inheritdoc/>
        public AudioResampler GetResampler()
        {
            return SoLoud.GetResampler();
        }

        /// <inheritdoc cref="SoLoud.getGlobalVolume"/>
        public float GetGlobalVolume()
        {
            return SoLoud.getGlobalVolume();
        }

        /// <inheritdoc cref="SoLoud.getMaxActiveVoiceCount"/>
        public uint GetMaxActiveVoiceCount()
        {
            return SoLoud.getMaxActiveVoiceCount();
        }

        /// <inheritdoc/>
        public uint GetActiveVoiceCount()
        {
            return SoLoud.GetActiveVoiceCount();
        }

        /// <inheritdoc cref="SoLoud.getVoiceCount"/>
        public uint GetVoiceCount()
        {
            return SoLoud.getVoiceCount();
        }

        /// <inheritdoc cref="SoLoud.getBackendString"/>
        public string? GetBackendString()
        {
            return SoLoud.getBackendString();
        }

        /// <inheritdoc cref="SoLoud.getBackendChannels"/>
        public uint GetBackendChannels()
        {
            return SoLoud.getBackendChannels();
        }

        /// <inheritdoc cref="SoLoud.getBackendSamplerate"/>
        public uint GetBackendSampleRate()
        {
            return SoLoud.getBackendSamplerate();
        }

        /// <inheritdoc cref="SoLoud.getBackendBufferSize"/>
        public uint GetBackendBufferSize()
        {
            return SoLoud.getBackendBufferSize();
        }

        /// <inheritdoc cref="SoLoud.getSpeakerPosition(uint, out Vector3)"/>
        public void GetSpeakerPosition(uint channel, out Vector3 position)
        {
            SoLoud.getSpeakerPosition(channel, out position);
        }
    }
}
