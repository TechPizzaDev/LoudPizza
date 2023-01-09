using LoudPizza.Core;

namespace LoudPizza.Sources.Streaming
{
    public partial class AudioStreamer
    {
        private sealed class SeekWorker : Worker
        {
            private AlignedFloatBuffer _scratch;

            public SeekWorker(AudioStreamer streamer) : base(streamer)
            {
                _scratch.init(SoLoud.SampleGranularity * 2 * SoLoud.MaxChannels, SoLoud.VECTOR_SIZE);
            }

            protected override bool ShouldWork(StreamedAudioStream stream)
            {
                return stream.NeedsToSeek;
            }

            protected override void Work(StreamedAudioStream stream)
            {
                stream.SeekWork(_scratch.AsSpan());
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                _scratch.destroy();
            }

            ~SeekWorker()
            {
                Dispose(disposing: false);
            }
        }
    }
}
