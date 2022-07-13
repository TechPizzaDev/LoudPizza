
namespace LoudPizza
{
    public abstract class AudioCollider
    {
        /// <summary>
        /// Calculate volume multiplier. Assumed to return value between 0 and 1.
        /// </summary>
        public abstract float collide(SoLoud aSoloud, AudioSourceInstance3dData aAudioInstance3dData, int aUserData);
    }
}
