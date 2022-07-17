using System;

namespace LoudPizza.Core
{
    public interface IAudioStream : IDisposable
    {
        /// <summary>
        /// Read samples from the stream to the buffer.
        /// </summary>
        /// <returns>The amount of samples read.</returns>
        uint GetAudio(Span<float> buffer, uint samplesToRead, uint bufferSize);

        /// <summary>
        /// Get whether the has stream ended.
        /// </summary>
        bool HasEnded();

        /// <summary>
        /// Seek to certain place in the stream.
        /// </summary>
        /// <remarks>
        /// Base implementation is a generic "tape" seek.
        /// </remarks>
        SoLoudStatus Seek(ulong samplePosition, Span<float> scratch);
    }
}
