
namespace LoudPizza.Core
{
    public abstract class AudioAttenuator
    {
        public abstract float Attenuate(float distance, float minDistance, float maxDistance, float rolloffFactor);
    }
}
