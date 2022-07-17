using LoudPizza.Core;

namespace LoudPizza
{
    /// <summary>
    /// Represents a wrapper handle around the library.
    /// </summary>
    public readonly partial struct SoLoudHandle : IAudioBus
    {
        /// <summary>
        /// Gets the main instance of the library associated with this handle.
        /// </summary>
        public SoLoud SoLoud { get; }

        public SoLoudHandle(SoLoud soLoud)
        {
            SoLoud = soLoud;
        }

        /// <inheritdoc/>
        public void CalcFFT(out Buffer256 buffer)
        {
            SoLoud.CalcFFT(out buffer);
        }

        /// <inheritdoc/>
        public void GetWave(out Buffer256 buffer)
        {
            SoLoud.GetWave(out buffer);
        }

        /// <inheritdoc/>
        public float GetApproximateVolume(uint channel)
        {
            return SoLoud.GetApproximateVolume(channel);
        }

        /// <inheritdoc/>
        public void GetApproximateVolumes(out ChannelBuffer buffer)
        {
            SoLoud.GetApproximateVolumes(out buffer);
        }

        /// <inheritdoc/>
        public void AnnexSound(Handle voiceHandle)
        {
            SoLoud.AnnexSound(voiceHandle);
        }

        /// <inheritdoc/>
        public Handle GetBusHandle()
        {
            return default;
        }

        /// <summary>
        /// Gets or sets the resampler for the main bus.
        /// </summary>
        public AudioResampler Resampler
        {
            get => GetResampler();
            set => SetResampler(value);
        }

        /// <summary>
        /// Gets or sets the global volume.
        /// </summary>
        public float GlobalVolume
        {
            get => GetGlobalVolume();
            set => SetGlobalVolume(value);
        }

        public static implicit operator SoLoud(SoLoudHandle soLoudHandle)
        {
            return soLoudHandle.SoLoud;
        }
    }
}
