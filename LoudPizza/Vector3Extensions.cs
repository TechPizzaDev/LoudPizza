using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LoudPizza
{
    public static class Vector3Extensions
    {
        /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SafeNormalize(Vector3 value)
        {
            float length = value.LengthSquared();
            if (length == 0)
            {
                return Vector3.Zero;
            }
            return value / MathF.Sqrt(length);
        }
    }
}
