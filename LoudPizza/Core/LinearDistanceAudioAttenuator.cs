
namespace LoudPizza
{
    /// <summary>
    /// Linear distance attenuation model.
    /// </summary>
    public class LinearDistanceAudioAttenuator : AudioAttenuator
    {
        public static LinearDistanceAudioAttenuator Instance { get; } = new LinearDistanceAudioAttenuator();

        public override float attenuate(float aDistance, float aMinDistance, float aMaxDistance, float aRolloffFactor)
        {
            return SoLoud.attenuateLinearDistance(aDistance, aMinDistance, aMaxDistance, aRolloffFactor);
        }
    }
}
