using System;
using System.Runtime.InteropServices;

namespace LoudPizza.Core
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
        public SoLoudStatus init(uint aFloats)
        {
            destroy();

            mData = null;
            mFloats = aFloats;
#if SSE_INTRINSICS
            mBasePtr = Marshal.AllocHGlobal((int)aFloats * sizeof(float) + 16);
            if (mBasePtr == IntPtr.Zero)
                return SoLoudStatus.OutOfMemory;
            mData = (float*)(((long)mBasePtr + 15) & ~15);
#else
            mBasePtr = Marshal.AllocHGlobal((int)aFloats * sizeof(float));
            if (mBasePtr == IntPtr.Zero)
                return SOLOUD_ERRORS.OUT_OF_MEMORY;
            mData = (float*)mBasePtr;
#endif
            return SoLoudStatus.Ok;
        }

        public Span<float> AsSpan()
        {
            if (mData == null)
            {
                throw new InvalidOperationException();
            }
            return new Span<float>(mData, (int)mFloats);
        }

        public Span<float> AsSpan(int start)
        {
            return AsSpan().Slice(start);
        }

        public Span<float> AsSpan(int start, int length)
        {
            return AsSpan().Slice(start, length);
        }

        public void destroy()
        {
            if (mBasePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(mBasePtr);
                mBasePtr = IntPtr.Zero;
                mData = null;
            }
        }
    }
}
