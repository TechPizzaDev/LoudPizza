using System;

namespace LoudPizza.Sources.Streaming
{
    public partial class AudioStreamer
    {
        private readonly struct StreamHolder : IEquatable<StreamHolder>
        {
            public StreamedAudioStream Stream { get; }
            public object Mutex { get; }

            public StreamHolder(StreamedAudioStream stream, object mutex)
            {
                Stream = stream;
                Mutex = mutex;
            }

            public bool Equals(StreamHolder other)
            {
                return ReferenceEquals(Stream, other.Stream);
            }
        }
    }
}
