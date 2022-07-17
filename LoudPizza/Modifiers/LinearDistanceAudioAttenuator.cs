using LoudPizza.Core;

namespace LoudPizza.Modifiers
{
    /// <summary>
    /// Linear distance attenuation model.
    /// </summary>
    public class LinearDistanceAudioAttenuator : AudioAttenuator
    {
        public static LinearDistanceAudioAttenuator Instance { get; } = new LinearDistanceAudioAttenuator();

        public override float Attenuate(float distance, float minDistance, float maxDistance, float rolloffFactor)
        {
            return SoLoud.attenuateLinearDistance(distance, minDistance, maxDistance, rolloffFactor);
        }
    }
}
