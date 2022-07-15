using System;
using System.Diagnostics;

namespace LoudPizza
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public readonly struct Time
    {
        public double Seconds { get; }

        public Time(double seconds)
        {
            Seconds = seconds;
        }

        public static implicit operator Time(TimeSpan timeSpan)
        {
            return new Time(timeSpan.TotalSeconds);
        }

        public static implicit operator Time(double seconds)
        {
            return new Time(seconds);
        }

        public static implicit operator double(Time time)
        {
            return time.Seconds;
        }

        public override string ToString()
        {
            return $"{Seconds}s";
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}
