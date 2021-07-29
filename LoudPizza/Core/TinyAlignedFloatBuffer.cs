namespace LoudPizza
{
    // Lightweight class that handles small aligned buffer to support vectorized operations
    unsafe struct TinyAlignedFloatBuffer
    {
        public const int Length = sizeof(float) * 16 + 16;

        public fixed byte mData[Length];

        public static float* align(byte* basePtr)
        {
            return (float*)(((long)basePtr + 15) & ~15);
        }
    }
}
