
namespace LoudPizza.Core
{
    /// <summary>
    /// Exponential distance attenuation model.
    /// </summary>
    public class ExponentialDistanceAudioAttenuator : AudioAttenuator
    {
        public static ExponentialDistanceAudioAttenuator Instance { get; } = new ExponentialDistanceAudioAttenuator();

        public override float Attenuate(float distance, float minDistance, float maxDistance, float rolloffFactor)
        {
            return SoLoud.attenuateExponentialDistance(distance, minDistance, maxDistance, rolloffFactor);
        }
    }
}
