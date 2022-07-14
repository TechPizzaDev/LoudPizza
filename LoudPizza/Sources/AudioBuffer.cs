
namespace LoudPizza
{
    public unsafe class AudioBuffer : AudioSource
    {
        //result loadwav(MemoryFile* aReader);
        //result loadogg(MemoryFile* aReader);
        //result loadmp3(MemoryFile* aReader);
        //result loadflac(MemoryFile* aReader);
        //result testAndLoadFile(MemoryFile* aReader);

        public float[] mData;
        public uint mSampleCount;

        public AudioBuffer()
        {
        }

        //SOLOUD_ERRORS load(const char* aFilename);
        //SOLOUD_ERRORS loadMem(const unsigned char* aMem, uint aLength, bool aCopy = false, bool aTakeOwnership = true);
        //SOLOUD_ERRORS loadFile(File* aFile);

        public SoLoudStatus loadRawWave8(byte* aMem, uint aLength, float aSamplerate , uint aChannels)
        {
            if (aMem == null || aLength == 0 || aSamplerate <= 0 || aChannels < 1)
                return SoLoudStatus.InvalidParameter;

            deleteData();
            float[] data = new float[aLength];
            mData = data;
            mSampleCount = aLength / aChannels;
            mChannels = aChannels;
            mBaseSamplerate = aSamplerate;
            for (uint i = 0; i < data.Length; i++)
                data[i] = (aMem[i] - 128) / (float)0x80;
            return SoLoudStatus.Ok;
        }

        public SoLoudStatus loadRawWave16(short* aMem, uint aLength, float aSamplerate, uint aChannels)
        {
            if (aMem == null || aLength == 0 || aSamplerate <= 0 || aChannels < 1)
                return SoLoudStatus.InvalidParameter;

            deleteData();
            float[] data = new float[aLength];
            mData = data;
            mSampleCount = aLength / aChannels;
            mChannels = aChannels;
            mBaseSamplerate = aSamplerate;
            for (uint i = 0; i < data.Length; i++)
                data[i] = aMem[i] / (float)0x8000;
            return SoLoudStatus.Ok;
        }

        public SoLoudStatus loadRawWave(float* aMem, uint aLength, float aSamplerate, uint aChannels, bool aTakeOwnership)
        {
            if (aMem == null || aLength == 0 || aSamplerate <= 0 || aChannels < 1)
                return SoLoudStatus.InvalidParameter;

            deleteData();
            if (aTakeOwnership == false)
            {
                mData = new float[aLength];
                CRuntime.memcpy(mData, 0, aMem, sizeof(float) * aLength);
            }
            else
            {
                throw new System.NotImplementedException();
                //mData = aMem;
            }
            mSampleCount = aLength / aChannels;
            mChannels = aChannels;
            mBaseSamplerate = aSamplerate;
            return SoLoudStatus.Ok;
        }

        public override AudioBufferInstance createInstance()
        {
            return new AudioBufferInstance(this);
        }

        public Time getLength()
        {
            if (mBaseSamplerate == 0)
                return 0;
            return mSampleCount / (double)mBaseSamplerate;
        }

        private void deleteData()
        {
            stop();
            //delete[] mData; 
        }

        protected override void Dispose(bool disposing)
        {
            deleteData();

            base.Dispose(disposing);
        }
    }
}
