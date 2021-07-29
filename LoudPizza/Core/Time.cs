using System.Diagnostics;

namespace LoudPizza
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public readonly struct Time
    {
        public double Value { get; }

        public Time(double value)
        {
            Value = value;
        }

        public static Time FromSeconds(double seconds)
        {
            return new Time(seconds);
        }

        public static Time operator +(Time a, Time b)
        {
            return a.Value + b.Value;
        }

        public static Time operator -(Time a, Time b)
        {
            return a.Value - b.Value;
        }

        public static implicit operator Time(double value)
        {
            return new Time(value);
        }

        public static implicit operator double(Time time)
        {
            return time.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}
