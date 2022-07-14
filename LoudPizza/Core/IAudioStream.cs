using System;

namespace LoudPizza
{
    public unsafe interface IAudioStream : IDisposable
    {
        /// <summary>
        /// Get samples from the stream to the buffer.
        /// </summary>
        /// <returns>The amount of samples written.</returns>
        uint getAudio(Span<float> aBuffer, uint aSamplesToRead, uint aBufferSize);

        /// <summary>
        /// Get whether the has stream ended.
        /// </summary>
        bool hasEnded();

        /// <summary>
        /// Seek to certain place in the stream.
        /// </summary>
        /// <remarks>
        /// Base implementation is generic "tape" seek (and slow).
        /// </remarks>
        SoLoudStatus seek(ulong aSamplePosition, Span<float> mScratch);
    }
}
