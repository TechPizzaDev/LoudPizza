using System;
using LoudPizza.Core;

namespace LoudPizza.Sources
{
    public class AudioStream : AudioSource
    {
        private IAudioStream? _audioStream;
        private AudioStreamInstance? _instance;

        public uint mSampleCount;

        public AudioStream(SoLoud soLoud, IAudioStream audioStream) : base(soLoud)
        {
            _audioStream = audioStream ?? throw new ArgumentNullException(nameof(audioStream));
        }

        public override AudioStreamInstance CreateInstance()
        {
            if (_instance != null)
            {
                Stop();
                _instance = null;
            }

            if (_audioStream == null)
            {
                ThrowObjectDisposed();
            }

            _instance = new AudioStreamInstance(this, _audioStream);
            _audioStream = null;
            return _instance;
        }

        
        public Time GetLength()
        {
            if (mBaseSamplerate == 0)
                return 0;
            return mSampleCount / mBaseSamplerate;
        }

        internal void ReturnAudioStream(AudioStreamInstance instance)
        {
            if (_instance != instance)
            {
                throw new InvalidOperationException("The given instance does not originate from this source.");
            }

            _audioStream = instance.DataStream;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _audioStream?.Dispose();
            _audioStream = null;
        }
    }
}
