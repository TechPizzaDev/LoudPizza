namespace LoudPizza.Sources.Streaming
{
    public partial class AudioStreamer
    {
        private sealed class ReadWorker : Worker
        {
            public ReadWorker(AudioStreamer streamer) : base(streamer)
            {
            }

            protected override bool ShouldWork(StreamedAudioStream stream)
            {
                return stream.NeedsToRead;
            }

            protected override void Work(StreamedAudioStream stream)
            {
                stream.ReadWork();
            }
        }
    }
}
