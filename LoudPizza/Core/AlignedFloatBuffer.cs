using System;
using System.Runtime.InteropServices;
using System.Threading;

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
        public SoLoudStatus init(uint aFloats, uint alignment)
        {
            destroy();

            mData = null;
            mFloats = aFloats;
#if !NET6_0_OR_GREATER
            mBasePtr = Marshal.AllocHGlobal((int)(aFloats * sizeof(float) + alignment));
            if (mBasePtr == IntPtr.Zero)
                return SoLoudStatus.OutOfMemory;
            mData = (float*)(((long)mBasePtr + (alignment - 1)) & ~(alignment - 1));
#else
            mBasePtr = (IntPtr)NativeMemory.AlignedAlloc(aFloats * sizeof(float), alignment);
            if (mBasePtr == IntPtr.Zero)
                return SoLoudStatus.OutOfMemory;
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
            IntPtr ptr = Interlocked.Exchange(ref mBasePtr, IntPtr.Zero);
            mData = null;

            if (ptr != IntPtr.Zero)
            {
#if NET6_0_OR_GREATER
                NativeMemory.AlignedFree((void*)ptr);
#else
                Marshal.FreeHGlobal(ptr);
#endif
            }
        }
    }
}
