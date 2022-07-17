using System;
using LoudPizza.Core;
using LoudPizza.Sources;

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

        /// <inheritdoc cref="SoLoud.postinit_internal(uint, uint, uint)"/>
        public void InitializeFromBackend(uint sampleRate, uint bufferSize, uint channels)
        {
            SoLoud.postinit_internal(sampleRate, bufferSize, channels);
        }

        /// <inheritdoc cref="SoLoud.deinit"/>
        public void Shutdown()
        {
            SoLoud.deinit();
        }

        /// <inheritdoc cref="SoLoud.mix(float*, uint)"/>
        /// <exception cref="ArgumentException">
        /// <paramref name="buffer"/> length is not a multiple of <paramref name="samples"/>.
        /// </exception>
        public unsafe void Mix(Span<float> buffer, uint samples)
        {
            fixed (float* bufferPtr = buffer)
            {
                SoLoud.mix(bufferPtr, samples);
            }
        }

        /// <inheritdoc cref="SoLoud.mixSigned16(short*, uint)"/>
        /// <exception cref="ArgumentException">
        /// <paramref name="buffer"/> length is not a multiple of <paramref name="samples"/>.
        /// </exception>
        public unsafe void MixSigned16(Span<short> buffer, uint samples)
        {
            if (buffer.Length % samples != 0)
            {
                throw new ArgumentException();
            }

            fixed (short* bufferPtr = buffer)
            {
                SoLoud.mixSigned16(bufferPtr, samples);
            }
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
