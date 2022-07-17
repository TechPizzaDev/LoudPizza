using LoudPizza.Core;

namespace LoudPizza.Modifiers
{
    public class LinearAudioResampler : AudioResampler
    {
        public static LinearAudioResampler Instance { get; } = new();

        public override unsafe void Resample(float* src0, float* src1, float* dst, int srcOffset, int dstSampleCount, int stepFixed)
        {
            SoLoud.resample_linear(src0, src1, dst, srcOffset, dstSampleCount, stepFixed);
        }
    }
}
