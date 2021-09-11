using System;

namespace LoudPizza
{
    public unsafe interface IAudioStream : IDisposable
    {
        // Get N samples from the stream to the buffer. Report samples written.
        uint getAudio(float* aBuffer, uint aSamplesToRead, uint aBufferSize);

        // Has the stream ended?
        bool hasEnded();

        // Seek to certain place in the stream. Base implementation is generic "tape" seek (and slow).
        SOLOUD_ERRORS seek(ulong aSamplePosition, float* mScratch, uint mScratchSize);
    }
}
