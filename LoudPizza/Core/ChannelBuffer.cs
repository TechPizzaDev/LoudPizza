namespace LoudPizza
{
    public unsafe struct ChannelBuffer
    {
        public fixed float Data[SoLoud.MAX_CHANNELS];

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
