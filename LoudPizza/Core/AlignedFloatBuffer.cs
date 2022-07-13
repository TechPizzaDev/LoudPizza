using System;
using System.Runtime.InteropServices;

namespace LoudPizza
{
    /// <summary>
    /// Handles aligned allocations to support vectorized operations.
    /// </summary>
    public unsafe struct AlignedFloatBuffer
    {
        /// <summary>
        /// Aligned pointer.
        /// </summary>
        public float* mData;

        /// <summary>
        /// Raw allocated pointer (for delete).
        /// </summary>
        public IntPtr mBasePtr;

        /// <summary>
        /// Size of buffer (w/out padding).
        /// </summary>
        public uint mFloats;

        /// <summary>
        /// Allocate and align buffer.
        /// </summary>
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

        /// <summary>
        /// Clear data to zero.
        /// </summary>
        public void clear()
        {
            CRuntime.memset(mData, 0, sizeof(float) * mFloats);
        }

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
