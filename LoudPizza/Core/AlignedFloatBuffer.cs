using System;
using System.Runtime.InteropServices;

namespace LoudPizza
{
    // Class that handles aligned allocations to support vectorized operations
    public unsafe struct AlignedFloatBuffer
    {
        public float* mData; // aligned pointer
        public IntPtr mBasePtr; // raw allocated pointer (for delete)
        public uint mFloats; // size of buffer (w/out padding)

        // Allocate and align buffer
        public SOLOUD_ERRORS init(uint aFloats)
        {
            destroy();

            mData = null;
            mFloats = aFloats;
#if SSE_INTRINSICS
            mBasePtr = Marshal.AllocHGlobal((int)aFloats * sizeof(float) + 16);
            if (mBasePtr == IntPtr.Zero)
                return SOLOUD_ERRORS.OUT_OF_MEMORY;
            mData = (float*)(((long)mBasePtr + 15) & ~15);
#else
            mBasePtr = Marshal.AllocHGlobal((int)aFloats * sizeof(float));
            if (mBasePtr == IntPtr.Zero)
                return SOLOUD_ERRORS.OUT_OF_MEMORY;
            mData = (float*)mBasePtr;
#endif
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        // Clear data to zero.
        public void clear()
        {
            CRuntime.memset(mData, 0, sizeof(float) * mFloats);
        }

        // dtor
        public void destroy()
        {
            if (mBasePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(mBasePtr);
                mBasePtr = IntPtr.Zero;
            }
        }
    }
}
