using System;

namespace LoudPizza
{
    [Flags]
    public enum AudioSeekFlags
    {
        None = 0,

        /// <summary>
        /// The seek operation can complete later.
        /// </summary>
        NonBlocking = 1 << 0,
    }
}
