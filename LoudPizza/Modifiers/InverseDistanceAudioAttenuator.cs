
namespace LoudPizza.Core
{
    /// <summary>
    /// Inverse distance attenuation model.
    /// </summary>
    public class InverseDistanceAudioAttenuator : AudioAttenuator
    {
        public static InverseDistanceAudioAttenuator Instance { get; } = new InverseDistanceAudioAttenuator();

        public override float Attenuate(float distance, float minDistance, float maxDistance, float rolloffFactor)
        {
            return SoLoud.attenuateInvDistance(distance, minDistance, maxDistance, rolloffFactor);
        }
    }
}
