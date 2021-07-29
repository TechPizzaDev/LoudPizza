
namespace LoudPizza
{
    public abstract class AudioCollider
    {
        // Calculate volume multiplier. Assumed to return value between 0 and 1.
        public abstract float collide(SoLoud aSoloud, AudioSourceInstance3dData aAudioInstance3dData, int aUserData);
    }
}
