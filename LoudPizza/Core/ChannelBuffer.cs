using System.Runtime.CompilerServices;

namespace LoudPizza
{
    public unsafe struct ChannelBuffer
    {
        public fixed float Data[SoLoud.MAX_CHANNELS];

        public float this[nint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Data[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Data[index] = value;
        }

        public float this[nuint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Data[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Data[index] = value;
        }
    }
}
