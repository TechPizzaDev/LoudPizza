using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct SoLoudHandle
    {
        /// <inheritdoc cref="SoLoud.createVoiceGroup"/>
        public VoiceHandle CreateVoiceGroup()
        {
            Handle handle = SoLoud.createVoiceGroup();
            return new VoiceHandle(SoLoud, handle);
        }
    }
}
