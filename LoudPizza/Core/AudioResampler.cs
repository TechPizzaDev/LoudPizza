
namespace LoudPizza.Core
{
    public unsafe abstract class AudioResampler
    {
        public abstract void resample(
            float* aSrc,
            float* aSrc1,
            float* aDst,
            int aSrcOffset,
            int aDstSampleCount,
            int aStepFixed);
    }
}
