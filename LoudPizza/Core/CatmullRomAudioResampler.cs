
namespace LoudPizza.Core
{
    public class CatmullRomAudioResampler : AudioResampler
    {
        public static CatmullRomAudioResampler Instance { get; } = new();

        public override unsafe void resample(float* aSrc, float* aSrc1, float* aDst, int aSrcOffset, int aDstSampleCount, int aStepFixed)
        {
            SoLoud.resample_catmullrom(aSrc, aSrc1, aDst, aSrcOffset, aDstSampleCount, aStepFixed);
        }
    }
}
