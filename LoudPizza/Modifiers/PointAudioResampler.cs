using System;
using LoudPizza.Core;

namespace LoudPizza.Modifiers
{
    public class PointAudioResampler : AudioResampler
    {
        public static PointAudioResampler Instance { get; } = new();

        public override unsafe void Resample(
            ReadOnlySpan<float> src0,
            ReadOnlySpan<float> src1,
            Span<float> dst,
            int srcOffset,
            int stepFixed)
        {
            fixed (float* src0Ptr = src0)
            fixed (float* src1Ptr = src1)
            fixed (float* dstPtr = dst)
            {
                SoLoud.resample_point(src0Ptr, src1Ptr, dstPtr, srcOffset, dst.Length, stepFixed);
            }
        }
    }
}
