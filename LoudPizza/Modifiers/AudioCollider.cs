using LoudPizza.Sources;

namespace LoudPizza.Core
{
    public abstract class AudioCollider
    {
        /// <summary>
        /// Calculate volume multiplier. Assumed to return value between 0 and 1.
        /// </summary>
        public abstract float Collide(SoLoud soLoud, in AudioSourceInstance3dData audioInstance3dData);
    }
}
