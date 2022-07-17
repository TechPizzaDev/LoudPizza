using System;
using LoudPizza.Core;

namespace LoudPizza.Sources
{
    public unsafe class AudioBuffer : AudioSource
    {
        //result loadwav(MemoryFile* aReader);
        //result loadogg(MemoryFile* aReader);
        //result loadmp3(MemoryFile* aReader);
        //result loadflac(MemoryFile* aReader);
        //result testAndLoadFile(MemoryFile* aReader);

        internal float[] mData;
        internal uint mSampleCount;

        public AudioBuffer(SoLoud soLoud) : base(soLoud)
        {
        }

        //SOLOUD_ERRORS load(const char* aFilename);
        //SOLOUD_ERRORS loadMem(const unsigned char* aMem, uint aLength, bool aCopy = false, bool aTakeOwnership = true);
        //SOLOUD_ERRORS loadFile(File* aFile);

        public SoLoudStatus LoadRawWave8(ReadOnlySpan<byte> memory, float sampleRate, uint channels)
        {
            if (memory.Length == 0 || sampleRate <= 0 || channels < 1)
                return SoLoudStatus.InvalidParameter;

            DeleteData();
            float[] data = new float[memory.Length];
            mData = data;
            mSampleCount = (uint)memory.Length / channels;
            mChannels = channels;
            mBaseSamplerate = sampleRate;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (memory[i] - 128) / (float)0x80;
            }
            return SoLoudStatus.Ok;
        }

        public SoLoudStatus LoadRawWave16(ReadOnlySpan<short> memory, float sampleRate, uint channels)
        {
            if (memory.Length == 0 || sampleRate <= 0 || channels < 1)
                return SoLoudStatus.InvalidParameter;

            DeleteData();
            float[] data = new float[memory.Length];
            mData = data;
            mSampleCount = (uint)memory.Length / channels;
            mChannels = channels;
            mBaseSamplerate = sampleRate;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = memory[i] / (float)0x8000;
            }
            return SoLoudStatus.Ok;
        }

        public SoLoudStatus LoadRawWave(ReadOnlySpan<float> memory, float sampleRate, uint channels)
        {
            if (memory.Length == 0 || sampleRate <= 0 || channels < 1)
                return SoLoudStatus.InvalidParameter;

            DeleteData();
            mData = memory.ToArray();

            mSampleCount = (uint)memory.Length / channels;
            mChannels = channels;
            mBaseSamplerate = sampleRate;
            return SoLoudStatus.Ok;
        }

        public override AudioBufferInstance CreateInstance()
        {
            return new AudioBufferInstance(this);
        }

        public Time GetLength()
        {
            if (mBaseSamplerate == 0)
                return 0;
            return mSampleCount / (double)mBaseSamplerate;
        }

        private void DeleteData()
        {
            Stop();
            //delete[] mData; 
        }

        protected override void Dispose(bool disposing)
        {
            DeleteData();

            base.Dispose(disposing);
        }
    }
}
