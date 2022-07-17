using System;

namespace LoudPizza.Core
{
    public unsafe interface IAudioStream : IDisposable
    {
        /// <summary>
        /// Get samples from the stream to the buffer.
        /// </summary>
        /// <returns>The amount of samples written.</returns>
        uint GetAudio(Span<float> aBuffer, uint aSamplesToRead, uint aBufferSize);

        /// <summary>
        /// Get whether the has stream ended.
        /// </summary>
        bool HasEnded();

        /// <summary>
        /// Seek to certain place in the stream.
        /// </summary>
        /// <remarks>
        /// Base implementation is generic "tape" seek (and slow).
        /// </remarks>
        SoLoudStatus Seek(ulong aSamplePosition, Span<float> mScratch);
    }
}
