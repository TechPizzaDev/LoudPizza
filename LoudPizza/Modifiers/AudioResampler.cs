using System;

namespace LoudPizza.Modifiers
{
    public unsafe abstract class AudioResampler
    {
        public abstract void Resample(
            ReadOnlySpan<float> src0,
            ReadOnlySpan<float> src1,
            Span<float> dst,
            int srcOffset,
            int stepFixed);
    }
}
