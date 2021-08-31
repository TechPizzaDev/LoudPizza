using System.IO;
using NLayer;

namespace LoudPizza
{
    public class Mp3Stream : AudioSource
    {
        private Mp3StreamInstance mp3Instance;

        public MpegFile mpegFile;

        public Mp3Stream(Stream stream, bool leaveOpen)
        {
            mpegFile = new MpegFile(stream, leaveOpen);

            mChannels = (uint)mpegFile.Channels;
            mBaseSamplerate = mpegFile.SampleRate;

            // TODO: allow multiple streams from the intial stream by buffering
            mp3Instance = new Mp3StreamInstance(this, mpegFile);
        }

        public override Mp3StreamInstance createInstance()
        {
            return mp3Instance;
            //return new Mp3StreamInstance(this, mpegFile);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                mp3Instance.Dispose();
                mpegFile.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
