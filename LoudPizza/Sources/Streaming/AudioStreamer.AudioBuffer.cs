using System;

namespace LoudPizza.Sources.Streaming
{
    public partial class AudioStreamer
    {
        internal class AudioBuffer
        {
            public float[] Buffer;
            public uint Length;
            public uint Start;

            public Span<float> AsSpan()
            {
                return Buffer.AsSpan();
            }
        }
    }
}
