using System;

namespace LoudPizza.Sources
{
    public interface IAudioStream : IDisposable
    {
        /// <summary>
        /// Gets the amount of channels in the stream.
        /// </summary>
        uint Channels { get; }

        /// <summary>
        /// Gets the sample rate of the stream.
        /// </summary>
        float SampleRate { get; }

        /// <summary>
        /// Gets the relative playback speed of the stream.
        /// </summary>
        float RelativePlaybackSpeed { get; }

        /// <summary>
        /// Reads non-interleaved samples into the specified buffer.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to read the samples into, 
        /// of which length must be a multiple of <see cref="Channels"/>.
        /// </param>
        /// <param name="samplesToRead">The amount of samples to read per channel.</param>
        /// <param name="channelStride">The offset in values between each channel in the buffer.</param>
        /// <returns>The amount of samples read.</returns>
        /// <remarks>
        /// The <paramref name="buffer"/> is not interleaved
        /// (Left, Left, Left, Right, Right, Right).
        /// </remarks>
        uint GetAudio(Span<float> buffer, uint samplesToRead, uint channelStride);

        /// <summary>
        /// Get whether the has stream ended.
        /// </summary>
        bool HasEnded();

        /// <summary>
        /// Get whether the stream is seekable.
        /// </summary>
        bool CanSeek();

        /// <summary>
        /// Attempt to seek to the given position in the stream.
        /// </summary>
        /// <param name="samplePosition">The target position to seek to.</param>
        /// <param name="scratch">Scratch buffer for seek implementations.</param>
        /// <param name="resultPosition">The position that the stream could seek to.</param>
        /// <returns>
        /// The status of the operation. 
        /// <see cref="SoLoudStatus.Ok"/> and <see cref="SoLoudStatus.EndOfStream"/> are considered non-errors.
        /// </returns>
        SoLoudStatus Seek(ulong samplePosition, Span<float> scratch, out ulong resultPosition);
    }
}
