
namespace LoudPizza
{
    public unsafe class AudioStreamInstance : AudioSourceInstance
    {
        public AudioStream mParent;
        public IAudioStream mStream;
        public uint mOffset;

        //public File* mFile;
        //public object mCodec;
        //public uint mOggFrameSize;
        //public uint mOggFrameOffset;
        //public float** mOggOutputs;
        //
        //int drflac_read_func(void* pUserData, void* pBufferOut, int bytesToRead)
        //{
        //    File* fp = (File*)pUserData;
        //    return fp->read(pBufferOut, bytesToRead);
        //}
        //
        //int drmp3_read_func(void* pUserData, void* pBufferOut, int bytesToRead)
        //{
        //    File* fp = (File*)pUserData;
        //    return fp->read(pBufferOut, bytesToRead);
        //}
        //
        //int drwav_read_func(void* pUserData, void* pBufferOut, int bytesToRead)
        //{
        //    File* fp = (File*)pUserData;
        //    return fp->read(pBufferOut, bytesToRead);
        //}
        //
        //bool drflac_seek_func(void* pUserData, int offset, drflac_seek_origin origin)
        //{
        //    File* fp = (File*)pUserData;
        //    if (origin != drflac_seek_origin_start)
        //        offset += fp->pos();
        //    fp->seek(offset);
        //    return true;
        //}
        //
        //bool drmp3_seek_func(void* pUserData, int offset, drmp3_seek_origin origin)
        //{
        //    File* fp = (File*)pUserData;
        //    if (origin != drmp3_seek_origin_start)
        //        offset += fp->pos();
        //    fp->seek(offset);
        //    return true;
        //}
        //
        //bool drwav_seek_func(void* pUserData, int offset, drwav_seek_origin origin)
        //{
        //    File* fp = (File*)pUserData;
        //    if (origin != drwav_seek_origin_start)
        //        offset += fp->pos();
        //    fp->seek(offset);
        //    return true;
        //}

        public AudioStreamInstance(AudioStream aParent, IAudioStream aStream)
        {
            mParent = aParent;
            mStream = aStream;
            mOffset = 0;

            //mCodec.mOgg = 0;
            //mCodec.mFlac = 0;
            //mFile = 0;
            //
            //if (aParent.mMemFile)
            //{
            //    MemoryFile* mf = new MemoryFile();
            //    mFile = mf;
            //    mf->openMem(aParent.mMemFile->getMemPtr(), aParent.mMemFile->length(), false, false);
            //}
            //else if (aParent.mFilename)
            //{
            //    DiskFile* df = new DiskFile;
            //    mFile = df;
            //    df->open(aParent.mFilename);
            //}
            //else if (aParent.mStreamFile)
            //{
            //    mFile = aParent.mStreamFile;
            //    mFile->seek(0); // stb_vorbis assumes file offset to be at start of ogg
            //}
            //else
            //{
            //    return;
            //}
            //
            //if (mFile)
            //{
            //    if (mParent.mFiletype == WAVSTREAM_WAV)
            //    {
            //        mCodec.mWav = new drwav;
            //        if (!drwav_init(mCodec.mWav, drwav_read_func, drwav_seek_func, (void*)mFile, null))
            //        {
            //            delete mCodec.mWav;
            //            mCodec.mWav = 0;
            //            if (mFile != mParent.mStreamFile)
            //                delete mFile;
            //            mFile = 0;
            //        }
            //    }
            //    else if (mParent.mFiletype == WAVSTREAM_OGG)
            //    {
            //        int e;
            //
            //        mCodec.mOgg = stb_vorbis_open_file((Soloud_Filehack*)mFile, 0, &e, 0);
            //
            //        if (!mCodec.mOgg)
            //        {
            //            if (mFile != mParent.mStreamFile)
            //                delete mFile;
            //            mFile = 0;
            //        }
            //        mOggFrameSize = 0;
            //        mOggFrameOffset = 0;
            //        mOggOutputs = 0;
            //    }
            //    else if (mParent.mFiletype == WAVSTREAM_FLAC)
            //    {
            //        mCodec.mFlac = drflac_open(drflac_read_func, drflac_seek_func, (void*)mFile, null);
            //        if (!mCodec.mFlac)
            //        {
            //            if (mFile != mParent.mStreamFile)
            //                delete mFile;
            //            mFile = 0;
            //        }
            //    }
            //    else if (mParent.mFiletype == WAVSTREAM_MP3)
            //    {
            //        mCodec.mMp3 = new drmp3;
            //        if (!drmp3_init(mCodec.mMp3, drmp3_read_func, drmp3_seek_func, (void*)mFile, null, null))
            //        {
            //            delete mCodec.mMp3;
            //            mCodec.mMp3 = 0;
            //            if (mFile != mParent.mStreamFile)
            //                delete mFile;
            //            mFile = 0;
            //        }
            //    }
            //    else
            //    {
            //        if (mFile != mParent.mStreamFile)
            //            delete mFile;
            //        mFile = null;
            //        return;
            //    }
            //}
        }

        ~AudioStreamInstance()
        {
            //switch (mParent.mFiletype)
            //{
            //    case WAVSTREAM_OGG:
            //        if (mCodec.mOgg)
            //        {
            //            stb_vorbis_close(mCodec.mOgg);
            //        }
            //        break;
            //
            //    case WAVSTREAM_FLAC:
            //        if (mCodec.mFlac)
            //        {
            //            drflac_close(mCodec.mFlac);
            //        }
            //        break;
            //
            //    case WAVSTREAM_MP3:
            //        if (mCodec.mMp3)
            //        {
            //            drmp3_uninit(mCodec.mMp3);
            //            delete mCodec.mMp3;
            //            mCodec.mMp3 = 0;
            //        }
            //        break;
            //
            //    case WAVSTREAM_WAV:
            //        if (mCodec.mWav)
            //        {
            //            drwav_uninit(mCodec.mWav);
            //            delete mCodec.mWav;
            //            mCodec.mWav = 0;
            //        }
            //        break;
            //}
            //if (mFile != mParent.mStreamFile)
            //{
            //    delete mFile;
            //}
        }

        //static int getOggData(float** aOggOutputs, float* aBuffer, int aSamples, int aPitch, int aFrameSize, int aFrameOffset, int aChannels)
        //{
        //    if (aFrameSize <= 0)
        //        return 0;
        //
        //    int samples = aSamples;
        //    if (aFrameSize - aFrameOffset < samples)
        //    {
        //        samples = aFrameSize - aFrameOffset;
        //    }
        //
        //    int i;
        //    for (i = 0; i < aChannels; i++)
        //    {
        //        memcpy(aBuffer + aPitch * i, aOggOutputs[i] + aFrameOffset, sizeof(float) * samples);
        //    }
        //    return samples;
        //}

        public override uint getAudio(float* aBuffer, uint aSamplesToRead, uint aBufferSize)
        {
            //uint offset = 0;
            //if (mFile == null)
            //    return 0;
            //
            //switch (mParent.mFiletype)
            //{
            //    case WAVSTREAM_FLAC:
            //    {
            //        uint i, j, k;
            //
            //        for (i = 0; i < aSamplesToRead; i += 512)
            //        {
            //            float tmp[512 * MAX_CHANNELS];
            //            uint blockSize = (aSamplesToRead - i) > 512 ? 512 : aSamplesToRead - i;
            //            offset += (uint)drflac_read_pcm_frames_f32(mCodec.mFlac, blockSize, tmp);
            //
            //            for (j = 0; j < blockSize; j++)
            //            {
            //                for (k = 0; k < mChannels; k++)
            //                {
            //                    aBuffer[k * aSamplesToRead + i + j] = tmp[j * mCodec.mFlac->channels + k];
            //                }
            //            }
            //        }
            //        mOffset += offset;
            //        return offset;
            //    }
            //
            //    case WAVSTREAM_MP3:
            //    {
            //        uint i, j, k;
            //
            //        for (i = 0; i < aSamplesToRead; i += 512)
            //        {
            //            float tmp[512 * MAX_CHANNELS];
            //            uint blockSize = (aSamplesToRead - i) > 512 ? 512 : aSamplesToRead - i;
            //            offset += (uint)drmp3_read_pcm_frames_f32(mCodec.mMp3, blockSize, tmp);
            //
            //            for (j = 0; j < blockSize; j++)
            //            {
            //                for (k = 0; k < mChannels; k++)
            //                {
            //                    aBuffer[k * aSamplesToRead + i + j] = tmp[j * mCodec.mMp3->channels + k];
            //                }
            //            }
            //        }
            //        mOffset += offset;
            //        return offset;
            //    }
            //
            //    case WAVSTREAM_OGG:
            //    {
            //        if (mOggFrameOffset < mOggFrameSize)
            //        {
            //            int b = getOggData(mOggOutputs, aBuffer, aSamplesToRead, aBufferSize, mOggFrameSize, mOggFrameOffset, mChannels);
            //            mOffset += b;
            //            offset += b;
            //            mOggFrameOffset += b;
            //        }
            //
            //        while (offset < aSamplesToRead)
            //        {
            //            mOggFrameSize = stb_vorbis_get_frame_float(mCodec.mOgg, null, &mOggOutputs);
            //            mOggFrameOffset = 0;
            //            int b = getOggData(mOggOutputs, aBuffer + offset, aSamplesToRead - offset, aBufferSize, mOggFrameSize, mOggFrameOffset, mChannels);
            //            mOffset += b;
            //            offset += b;
            //            mOggFrameOffset += b;
            //
            //            if (mOffset >= mParent.mSampleCount || b == 0)
            //            {
            //                mOffset += offset;
            //                return offset;
            //            }
            //        }
            //    }
            //    break;
            //
            //    case WAVSTREAM_WAV:
            //    {
            //        uint i, j, k;
            //
            //        for (i = 0; i < aSamplesToRead; i += 512)
            //        {
            //            float tmp[512 * MAX_CHANNELS];
            //            uint blockSize = (aSamplesToRead - i) > 512 ? 512 : aSamplesToRead - i;
            //            offset += (uint)drwav_read_pcm_frames_f32(mCodec.mWav, blockSize, tmp);
            //
            //            for (j = 0; j < blockSize; j++)
            //            {
            //                for (k = 0; k < mChannels; k++)
            //                {
            //                    aBuffer[k * aSamplesToRead + i + j] = tmp[j * mCodec.mWav->channels + k];
            //                }
            //            }
            //        }
            //        mOffset += offset;
            //        return offset;
            //    }
            //}
            //return aSamplesToRead;
            return mStream.getAudio(aBuffer, aSamplesToRead, aBufferSize);
        }

        public override unsafe SOLOUD_ERRORS seek(Time aSeconds, float* mScratch, uint mScratchSize)
        {
            return mStream.seek(aSeconds, mScratch, mScratchSize);
        }

        public override SOLOUD_ERRORS rewind()
        {
            //switch (mParent.mFiletype)
            //{
            //    case WAVSTREAM_OGG:
            //        if (mCodec.mOgg)
            //        {
            //            stb_vorbis_seek_start(mCodec.mOgg);
            //        }
            //        break;
            //
            //    case WAVSTREAM_FLAC:
            //        if (mCodec.mFlac)
            //        {
            //            drflac_seek_to_pcm_frame(mCodec.mFlac, 0);
            //        }
            //        break;
            //
            //    case WAVSTREAM_MP3:
            //        if (mCodec.mMp3)
            //        {
            //            drmp3_seek_to_pcm_frame(mCodec.mMp3, 0);
            //        }
            //        break;
            //
            //    case WAVSTREAM_WAV:
            //        if (mCodec.mWav)
            //        {
            //            drwav_seek_to_pcm_frame(mCodec.mWav, 0);
            //        }
            //        break;
            //}
            //mOffset = 0;
            //mStreamPosition = 0.0f;
            //return 0;
            return mStream.rewind();
        }

        public override bool hasEnded()
        {
            return mStream.hasEnded();
        }
    }
}
