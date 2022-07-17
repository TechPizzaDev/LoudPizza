using LoudPizza.Core;

namespace LoudPizza
{
    public readonly partial struct VoiceHandle
    {
        /// <inheritdoc cref="SoLoud.isVoiceGroup(Handle)"/>
        public bool IsVoiceGroup => SoLoud.isVoiceGroup(Handle);

        /// <inheritdoc cref="SoLoud.isVoiceGroupEmpty(Handle)"/>
        public bool IsVoiceGroupEmpty => SoLoud.isVoiceGroupEmpty(Handle);

        /// <inheritdoc cref="SoLoud.addVoiceToGroup(Handle, Handle)"/>
        public SoLoudStatus AddVoiceToGroup(Handle voiceHandle)
        {
            return SoLoud.addVoiceToGroup(Handle, voiceHandle);
        }

        /// <inheritdoc cref="SoLoud.destroyVoiceGroup(Handle)"/>
        public SoLoudStatus DestroyVoiceGroup()
        {
            return SoLoud.destroyVoiceGroup(Handle);
        }
    }
}
