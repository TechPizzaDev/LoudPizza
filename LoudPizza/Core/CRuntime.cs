using System;
using System.Runtime.CompilerServices;

namespace LoudPizza
{
    public static unsafe class CRuntime
    {
        public static void memset(void* ptr, byte value, uint byteCount)
        {
            Unsafe.InitBlockUnaligned(ptr, value, byteCount);
        }

        public static void memset<T>(T[] ptr, byte value, uint byteCount)
            where T : unmanaged
        {
            Unsafe.InitBlockUnaligned(ref Unsafe.As<T, byte>(ref ptr[0]), value, byteCount);
        }

        public static void memcpy<T>(T[] destination, uint destinationByteOffset, void* source, uint byteCount)
            where T : unmanaged
        {
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.AddByteOffset(ref Unsafe.As<T, byte>(ref destination[0]), (IntPtr)destinationByteOffset),
                ref Unsafe.AsRef<byte>(source),
                byteCount);
        }

        public static void memcpy<T>(void* destination, T[] source, uint sourceByteOffset, uint byteCount)
            where T : unmanaged
        {
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.AsRef<byte>(destination),
                ref Unsafe.AddByteOffset(ref Unsafe.As<T, byte>(ref source[0]), (IntPtr)sourceByteOffset),
                byteCount);
        }

        public static void memcpy(void* destination, void* source, uint byteCount)
        {
            Unsafe.CopyBlockUnaligned(destination, source, byteCount);
        }

        public static void SkipInit<T>(out T value)
        {
            Unsafe.SkipInit(out value);
        }
    }
}
