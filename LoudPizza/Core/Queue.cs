
namespace LoudPizza
{
    public class Queue : AudioSource
    {
        public uint mReadIndex, mWriteIndex, mCount;
        public AudioSourceInstance?[] mSource;
        public QueueInstance? mInstance;
        public Handle mQueueHandle;

        public Queue(int capacity)
        {
            mQueueHandle = default;
            mInstance = null;
            mReadIndex = 0;
            mWriteIndex = 0;
            mCount = 0;
            mSource = new AudioSourceInstance[capacity];
        }

        public override QueueInstance createInstance()
        {
            if (mInstance != null)
            {
                stop();
                mInstance = null;
            }
            mInstance = new QueueInstance(this);
            return mInstance;
        }

        /// <summary>
        /// Play sound through the queue.
        /// </summary>
        public SOLOUD_ERRORS play(AudioSource aSound)
        {
            if (mSoloud == null)
            {
                return SOLOUD_ERRORS.INVALID_PARAMETER;
            }

            findQueueHandle();

            if (mQueueHandle.Value == 0)
                return SOLOUD_ERRORS.INVALID_PARAMETER;

            if (mCount >= mSource.Length)
                return SOLOUD_ERRORS.OUT_OF_MEMORY;

            if (aSound.mAudioSourceID == 0)
            {
                aSound.mAudioSourceID = mSoloud.mAudioSourceID;
                mSoloud.mAudioSourceID++;
            }

            AudioSourceInstance instance = aSound.createInstance();

            if (instance == null)
            {
                return SOLOUD_ERRORS.OUT_OF_MEMORY;
            }
            instance.init(aSound, 0);
            instance.mAudioSourceID = aSound.mAudioSourceID;

            mSoloud.lockAudioMutex_internal();
            mSource[mWriteIndex] = instance;
            mWriteIndex = (mWriteIndex + 1) % (uint)mSource.Length;
            mCount++;
            mSoloud.unlockAudioMutex_internal();

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        /// <summary>
        /// Get the number of audio sources queued for replay.
        /// </summary>
        public uint getQueueCount()
        {
            if (mSoloud == null)
            {
                return 0;
            }
            uint count;
            mSoloud.lockAudioMutex_internal();
            count = mCount;
            mSoloud.unlockAudioMutex_internal();
            return count;
        }

        /// <summary>
        /// Get whether the given audio source currently playing.
        /// </summary>
        public bool isCurrentlyPlaying(AudioSource aSound)
        {
            if (mSoloud == null || mCount == 0 || aSound.mAudioSourceID == 0)
                return false;

            mSoloud.lockAudioMutex_internal();
            bool res = mSource[mReadIndex]!.mAudioSourceID == aSound.mAudioSourceID;
            mSoloud.unlockAudioMutex_internal();
            return res;
        }

        /// <summary>
        /// Set params by reading them from the given audio source.
        /// </summary>
        public SOLOUD_ERRORS setParamsFromAudioSource(AudioSource aSound)
        {
            mChannels = aSound.mChannels;
            mBaseSamplerate = aSound.mBaseSamplerate;

            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        /// <summary>
        /// Set params manually.
        /// </summary>
        public SOLOUD_ERRORS setParams(float aSamplerate, uint aChannels = 2)
        {
            if (aChannels < 1 || aChannels > SoLoud.MAX_CHANNELS)
                return SOLOUD_ERRORS.INVALID_PARAMETER;

            mChannels = aChannels;
            mBaseSamplerate = aSamplerate;
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        /// <summary>
        /// Find the channel the queue is playing on to calculate handle.
        /// </summary>
        internal void findQueueHandle()
        {
            for (uint i = 0; mQueueHandle.Value == 0 && i < mSoloud.mHighestVoice; i++)
            {
                if (mSoloud.mVoice[i] == mInstance)
                {
                    mQueueHandle = mSoloud.getHandleFromVoice_internal(i);
                }
            }
        }
    }
}
