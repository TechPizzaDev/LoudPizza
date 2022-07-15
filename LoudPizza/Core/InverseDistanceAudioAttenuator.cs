
namespace LoudPizza.Core
{
    /// <summary>
    /// Inverse distance attenuation model.
    /// </summary>
    public class InverseDistanceAudioAttenuator : AudioAttenuator
    {
        public static InverseDistanceAudioAttenuator Instance { get; } = new InverseDistanceAudioAttenuator();

        public override float attenuate(float aDistance, float aMinDistance, float aMaxDistance, float aRolloffFactor)
        {
            return SoLoud.attenuateInvDistance(aDistance, aMinDistance, aMaxDistance, aRolloffFactor);
        }
    }
}
