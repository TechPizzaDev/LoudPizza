
namespace LoudPizza
{
    public unsafe struct Buffer256
    {
        public const int Length = 256;

        public fixed float Data[Length];

        public float this[int index]
        {
            get => Data[index];
            set => Data[index] = value;
        }

        public float this[uint index]
        {
            get => Data[index];
            set => Data[index] = value;
        }
    }
}
