
namespace LoudPizza
{
    public abstract class AudioAttenuator
    {
        public abstract float attenuate(float aDistance, float aMinDistance, float aMaxDistance, float aRolloffFactor);
    }
}
