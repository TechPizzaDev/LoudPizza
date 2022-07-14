using System.Runtime.CompilerServices;

namespace LoudPizza
{
    public static unsafe class CRuntime
    {
        public static void SkipInit<T>(out T value)
        {
            Unsafe.SkipInit(out value);
        }
    }
}
