using System;
using System.Diagnostics;

namespace LoudPizza
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public readonly struct Handle : IEquatable<Handle>
    {
        public uint Value { get; }

        public Handle(uint value)
        {
            Value = value;
        }

        public bool Equals(Handle other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is Handle other && Equals(other);
        }

        public static bool operator ==(Handle left, Handle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Handle left, Handle right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"0x{Value:x}";
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}
