namespace LoudPizza
{
    public unsafe abstract class AudioStream : AudioSource
    {
        //public int mFiletype;
        //public string? mFilename;
        //public File* mMemFile;
        //public File* mStreamFile;
        public uint mSampleCount;

        public AudioStream()
        {
            //mFilename = null;
            //mSampleCount = 0;
            //mFiletype = WAVSTREAM_WAV;
            //mMemFile = 0;
            //mStreamFile = 0;
        }

        ~AudioStream()
        {
            stop();
            //delete[] mFilename;
            //delete mMemFile;
        }

        /*
        public SOLOUD_ERRORS loadwav(File* fp)
        {
            fp->seek(0);
            drwav decoder;

            if (!drwav_init(&decoder, drwav_read_func, drwav_seek_func, (void*)fp, null))
                return FILE_LOAD_FAILED;

            mChannels = decoder.channels;
            if (mChannels > MAX_CHANNELS)
            {
                mChannels = MAX_CHANNELS;
            }

            mBaseSamplerate = (float)decoder.sampleRate;
            mSampleCount = (uint)decoder.totalPCMFrameCount;
            mFiletype = WAVSTREAM_WAV;
            drwav_uninit(&decoder);

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        public SOLOUD_ERRORS loadogg(File* fp)
        {
            fp->seek(0);
            int e;
            stb_vorbis* v;
            v = stb_vorbis_open_file((Soloud_Filehack*)fp, 0, &e, 0);
            if (v == null)
                return FILE_LOAD_FAILED;
            stb_vorbis_info info = stb_vorbis_get_info(v);
            mChannels = info.channels;
            if (info.channels > MAX_CHANNELS)
            {
                mChannels = MAX_CHANNELS;
            }
            mBaseSamplerate = (float)info.sample_rate;
            int samples = stb_vorbis_stream_length_in_samples(v);
            stb_vorbis_close(v);
            mFiletype = WAVSTREAM_OGG;

            mSampleCount = samples;

            return 0;
        }

        public SOLOUD_ERRORS loadflac(File* fp)
        {
            fp->seek(0);
            drflac* decoder = drflac_open(drflac_read_func, drflac_seek_func, (void*)fp, null);

            if (decoder == null)
                return FILE_LOAD_FAILED;

            mChannels = decoder->channels;
            if (mChannels > MAX_CHANNELS)
            {
                mChannels = MAX_CHANNELS;
            }

            mBaseSamplerate = (float)decoder->sampleRate;
            mSampleCount = (uint)decoder->totalPCMFrameCount;
            mFiletype = WAVSTREAM_FLAC;
            drflac_close(decoder);

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        public SOLOUD_ERRORS loadmp3(File* fp)
        {
            fp->seek(0);
            drmp3 decoder;
            if (!drmp3_init(&decoder, drmp3_read_func, drmp3_seek_func, (void*)fp, null, null))
                return FILE_LOAD_FAILED;


            mChannels = decoder.channels;
            if (mChannels > MAX_CHANNELS)
            {
                mChannels = MAX_CHANNELS;
            }

            drmp3_uint64 samples = drmp3_get_pcm_frame_count(&decoder);

            mBaseSamplerate = (float)decoder.sampleRate;
            mSampleCount = (uint)samples;
            mFiletype = WAVSTREAM_MP3;
            drmp3_uninit(&decoder);

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        public SOLOUD_ERRORS load(string aFilename)
        {
            //delete[] mFilename;
            delete mMemFile;
            mMemFile = 0;
            mFilename = aFilename;
            mSampleCount = 0;
            DiskFile fp;
            SOLOUD_ERRORS res = fp.open(aFilename);
            if (res != SOLOUD_ERRORS.SO_NO_ERROR)
                return res;

            //int len = (int)strlen(aFilename);
            //mFilename = new char[len + 1];
            //memcpy(mFilename, aFilename, len);
            //mFilename[len] = 0;

            res = parse(&fp);

            if (res != SOLOUD_ERRORS.SO_NO_ERROR)
            {
                //delete[] mFilename;
                //mFilename = 0;
                return res;
            }

            return 0;
        }

        public SOLOUD_ERRORS loadMem(byte* aData, uint aDataLen, bool aCopy, bool aTakeOwnership)
        {
            //delete[] mFilename;
            //delete mMemFile;
            mStreamFile = 0;
            mMemFile = 0;
            mFilename = null;
            mSampleCount = 0;

            if (aData == null || aDataLen == 0)
                return SOLOUD_ERRORS.INVALID_PARAMETER;

            MemoryFile* mf = new MemoryFile();
            SOLOUD_ERRORS res = mf->openMem(aData, aDataLen, aCopy, aTakeOwnership);
            if (res != SOLOUD_ERRORS.SO_NO_ERROR)
            {
                //delete mf;
                return res;
            }

            res = parse(mf);

            if (res != SOLOUD_ERRORS.SO_NO_ERROR)
            {
                //delete mf;
                return res;
            }

            mMemFile = mf;

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        public SOLOUD_ERRORS loadToMem(string aFilename)
        {
            DiskFile df;
            int res = df.open(aFilename);
            if (res == SOLOUD_ERRORS.SO_NO_ERROR)
            {
                res = loadFileToMem(&df);
            }
            return res;
        }

        public SOLOUD_ERRORS loadFile(File* aFile)
        {
            //delete[] mFilename;
            delete mMemFile;
            mStreamFile = 0;
            mMemFile = 0;
            mFilename = null;
            mSampleCount = 0;

            int res = parse(aFile);

            if (res != SOLOUD_ERRORS.SO_NO_ERROR)
            {
                return res;
            }

            mStreamFile = aFile;

            return 0;
        }

        public SOLOUD_ERRORS loadFileToMem(File* aFile)
        {
            //delete[] mFilename;
            delete mMemFile;
            mStreamFile = 0;
            mMemFile = 0;
            mFilename = null;
            mSampleCount = 0;

            MemoryFile* mf = new MemoryFile();
            SOLOUD_ERRORS res = mf->openFileToMem(aFile);
            if (res != SOLOUD_ERRORS.SO_NO_ERROR)
            {
                //delete mf;
                return res;
            }

            res = parse(mf);

            if (res != SOLOUD_ERRORS.SO_NO_ERROR)
            {
                //delete mf;
                return res;
            }

            mMemFile = mf;

            return res;
        }


        public SOLOUD_ERRORS parse(File* aFile)
        {
            int tag = aFile->read32();
            SOLOUD_ERRORS res = SOLOUD_ERRORS.SO_NO_ERROR;
            if (tag == MAKEDWORD('O', 'g', 'g', 'S'))
            {
                res = loadogg(aFile);
            }
            else
            if (tag == MAKEDWORD('R', 'I', 'F', 'F'))
            {
                res = loadwav(aFile);
            }
            else
            if (tag == MAKEDWORD('f', 'L', 'a', 'C'))
            {
                res = loadflac(aFile);
            }
            else
            if (loadmp3(aFile) == SO_NO_ERROR)
            {
                res = SOLOUD_ERRORS.SO_NO_ERROR;
            }
            else
            {
                res = SOLOUD_ERRORS.FILE_LOAD_FAILED;
            }
            return res;
        }
        */

        public Time getLength()
        {
            if (mBaseSamplerate == 0)
                return 0;
            return mSampleCount / mBaseSamplerate;
        }
    }
}
