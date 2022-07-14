
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
        public SoLoudStatus play(AudioSource aSound)
        {
            if (mSoloud == null)
            {
                return SoLoudStatus.InvalidParameter;
            }

            findQueueHandle();

            if (mQueueHandle.Value == 0)
                return SoLoudStatus.InvalidParameter;

            if (mCount >= mSource.Length)
                return SoLoudStatus.OutOfMemory;

            if (aSound.mAudioSourceID == 0)
            {
                aSound.mAudioSourceID = mSoloud.mAudioSourceID;
                mSoloud.mAudioSourceID++;
            }

            AudioSourceInstance instance = aSound.createInstance();

            if (instance == null)
            {
                return SoLoudStatus.OutOfMemory;
            }
            instance.init(aSound, 0);
            instance.mAudioSourceID = aSound.mAudioSourceID;

            lock (mSoloud.mAudioThreadMutex)
            {
                mSource[mWriteIndex] = instance;
                mWriteIndex = (mWriteIndex + 1) % (uint)mSource.Length;
                mCount++;
            }
            return SoLoudStatus.Ok;
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

            lock (mSoloud.mAudioThreadMutex)
            {
                uint count = mCount;
                return count;
            }
        }

        /// <summary>
        /// Get whether the given audio source currently playing.
        /// </summary>
        public bool isCurrentlyPlaying(AudioSource aSound)
        {
            if (mSoloud == null || mCount == 0 || aSound.mAudioSourceID == 0)
                return false;

            lock (mSoloud.mAudioThreadMutex)
            {
                bool res = mSource[mReadIndex]!.mAudioSourceID == aSound.mAudioSourceID;
                return res;
            }
        }

        /// <summary>
        /// Set params by reading them from the given audio source.
        /// </summary>
        public SoLoudStatus setParamsFromAudioSource(AudioSource aSound)
        {
            mChannels = aSound.mChannels;
            mBaseSamplerate = aSound.mBaseSamplerate;

            return SoLoudStatus.Ok;
        }

        /// <summary>
        /// Set params manually.
        /// </summary>
        public SoLoudStatus setParams(float aSamplerate, uint aChannels = 2)
        {
            if (aChannels < 1 || aChannels > SoLoud.MaxChannels)
                return SoLoudStatus.InvalidParameter;

            mChannels = aChannels;
            mBaseSamplerate = aSamplerate;
            return SoLoudStatus.Ok;
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
