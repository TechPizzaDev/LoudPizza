
namespace LoudPizza.Core
{
    public class CatmullRomAudioResampler : AudioResampler
    {
        public static CatmullRomAudioResampler Instance { get; } = new();

        public override unsafe void Resample(float* src0, float* src1, float* dst, int srcOffset, int dstSampleCount, int stepFixed)
        {
            SoLoud.resample_catmullrom(src0, src1, dst, srcOffset, dstSampleCount, stepFixed);
        }
    }
}
