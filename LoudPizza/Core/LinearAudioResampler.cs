
namespace LoudPizza.Core
{
    public class LinearAudioResampler : AudioResampler
    {
        public static LinearAudioResampler Instance { get; } = new();

        public override unsafe void resample(float* aSrc, float* aSrc1, float* aDst, int aSrcOffset, int aDstSampleCount, int aStepFixed)
        {
            SoLoud.resample_linear(aSrc, aSrc1, aDst, aSrcOffset, aDstSampleCount, aStepFixed);
        }
    }
}
