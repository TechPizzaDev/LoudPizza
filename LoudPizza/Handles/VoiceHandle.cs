using System.IO;
using LoudPizza.Core;

namespace LoudPizza
{
    /// <summary>
    /// Represents a wrapper handle around a voice handle.
    /// </summary>
    public readonly partial struct VoiceHandle
    {
        /// <summary>
        /// Gets the main instance of the library associated with this handle.
        /// </summary>
        public SoLoud SoLoud { get; }

        /// <summary>
        /// Gets the internal handle of the voice used by the library.
        /// </summary>
        public Handle Handle { get; }

        public VoiceHandle(SoLoud soLoud, Handle handle)
        {
            SoLoud = soLoud;
            Handle = handle;
        }

        /// <inheritdoc cref="SoLoud.seek(Handle, ulong)"/>
        public SoLoudStatus Seek(ulong samplePosition)
        {
            return SoLoud.seek(Handle, samplePosition);
        }

        /// <inheritdoc cref="SoLoud.stop(Handle)"/>
        public void Stop()
        {
            SoLoud.stop(Handle);
        }

        /// <summary>
        /// Gets or sets whether the voice is looping.
        /// </summary>
        /// <remarks>
        /// The audio source may not support looping (seeking).
        /// </remarks>
        public bool IsLooping
        {
            get => GetLooping();
            set => SetLooping(value);
        }

        /// <summary>
        /// Gets or sets whether the voice is protected.
        /// </summary>
        /// <remarks>
        /// Protected voices are not killed when many voices are playing.
        /// </remarks>
        public bool IsProtected
        {
            get => GetProtectVoice();
            set => SetProtectVoice(value);
        }

        /// <summary>
        /// Gets or sets the loop point of the voice.
        /// </summary>
        public ulong LoopPoint
        {
            get => GetLoopPoint();
            set => SetLoopPoint(value);
        }

        /// <summary>
        /// Gets or sets whether the voice is paused.
        /// </summary>
        public bool IsPaused
        {
            get => GetPause();
            set => SetPause(value);
        }

        /// <summary>
        /// Gets or sets the relative play speed of the voice.
        /// </summary>
        public float RelativePlaySpeed
        {
            get => GetRelativePlaySpeed();
            set => SetRelativePlaySpeed(value);
        }

        /// <summary>
        /// Gets or sets the channel pan of the voice.
        /// </summary>
        public float Pan
        {
            get => GetPan();
            set => SetPan(value);
        }

        /// Gets or sets the volume of the voice.
        public float Volume
        {
            get => GetVolume();
            set => SetVolume(value);
        }

        /// Gets or sets the sample rate of the voice.
        public float SampleRate
        {
            get => GetSampleRate();
            set => SetSampleRate(value);
        }

        /// <summary>
        /// Gets or sets whether the voice will automatically stop when it ends.
        /// </summary>
        public bool AutoStop
        {
            get => GetAutoStop();
            set => SetAutoStop(value);
        }

        /// <summary>
        /// Gets or sets the sample position within the stream.
        /// </summary>
        /// <remarks>
        /// The audio source may not support seeking.
        /// </remarks>
        /// <exception cref="IOException">Failed to seek.</exception>
        public ulong StreamSamplePosition
        {
            get => GetStreamSamplePosition();
            set
            {
                SoLoudStatus status = Seek(value);
                if (status != SoLoudStatus.Ok &&
                    status != SoLoudStatus.EndOfStream)
                {
                    throw new IOException(status.ToString());
                }
            }
        }

        public static implicit operator Handle(VoiceHandle voiceHandle)
        {
            return voiceHandle.Handle;
        }
    }
}
