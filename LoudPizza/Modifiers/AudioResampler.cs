
namespace LoudPizza.Modifiers
{
    public unsafe abstract class AudioResampler
    {
        public abstract void Resample(
            float* src0,
            float* src1,
            float* dst,
            int srcOffset,
            int dstSampleCount,
            int stepFixed);
    }
}
