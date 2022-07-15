
namespace LoudPizza.Core
{
    public class PointAudioResampler : AudioResampler
    {
        public static PointAudioResampler Instance { get; } = new();

        public override unsafe void resample(float* aSrc, float* aSrc1, float* aDst, int aSrcOffset, int aDstSampleCount, int aStepFixed)
        {
            SoLoud.resample_point(aSrc, aSrc1, aDst, aSrcOffset, aDstSampleCount, aStepFixed);
        }
    }
}
