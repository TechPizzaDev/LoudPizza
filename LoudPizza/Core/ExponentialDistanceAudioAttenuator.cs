
namespace LoudPizza
{
    // Exponential distance attenuation model
    public class ExponentialDistanceAudioAttenuator : AudioAttenuator
    {
        public static ExponentialDistanceAudioAttenuator Instance { get; } = new ExponentialDistanceAudioAttenuator();

        public override float attenuate(float aDistance, float aMinDistance, float aMaxDistance, float aRolloffFactor)
        {
            return SoLoud.attenuateExponentialDistance(aDistance, aMinDistance, aMaxDistance, aRolloffFactor);
        }
    }
}
