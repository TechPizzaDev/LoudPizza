using System.Runtime.CompilerServices;

namespace LoudPizza.Core
{
    public unsafe struct Buffer256
    {
        public const int Length = 256;

        public fixed float Data[Length];

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
