using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct SoLoudHandle
    {
        /// <inheritdoc cref="SoLoud.fadeGlobalVolume(float, Time)"/>
        public void FadeGlobalVolume(float to, Time time)
        {
            SoLoud.fadeGlobalVolume(to, time);
        }

        /// <inheritdoc cref="SoLoud.oscillateGlobalVolume(float, float, Time)"/>
        public void OscillateGlobalVolume(float from, float to, Time time)
        {
            SoLoud.oscillateGlobalVolume(from, to, time);
        }
    }
}
