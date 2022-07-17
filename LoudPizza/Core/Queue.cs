
namespace LoudPizza.Core
{
    public class Queue : AudioSource
    {
        internal uint mReadIndex;
        internal uint mWriteIndex;
        internal uint mCount;
        internal AudioSourceInstance?[] mSource;
        internal QueueInstance? mInstance;
        internal Handle mQueueHandle;

        public Queue(SoLoud soLoud, int capacity) : base(soLoud)
        {
            mQueueHandle = default;
            mInstance = null;
            mReadIndex = 0;
            mWriteIndex = 0;
            mCount = 0;
            mSource = new AudioSourceInstance[capacity];
        }

        public override QueueInstance CreateInstance()
        {
            if (mInstance != null)
            {
                Stop();
                mInstance = null;
            }
            mInstance = new QueueInstance(this);
            return mInstance;
        }

        /// <summary>
        /// Play the audio source through the queue.
        /// </summary>
        public SoLoudStatus Play(AudioSource source)
        {
            if (SoLoud == null)
                return SoLoudStatus.InvalidParameter;
            
            Handle queueHandle = FindQueueHandle();
            if (queueHandle.Value == 0)
                return SoLoudStatus.InvalidParameter;

            if (mCount >= mSource.Length)
                return SoLoudStatus.OutOfMemory;

            AudioSourceInstance instance = source.CreateInstance();
            if (instance == null)
            {
                return SoLoudStatus.OutOfMemory;
            }
            instance.init(0);

            lock (SoLoud.mAudioThreadMutex)
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
        public uint GetQueueCount()
        {
            if (SoLoud == null)
            {
                return 0;
            }

            lock (SoLoud.mAudioThreadMutex)
            {
                uint count = mCount;
                return count;
            }
        }

        /// <summary>
        /// Get whether the given audio source currently playing.
        /// </summary>
        public bool IsCurrentlyPlaying(AudioSource source)
        {
            if (SoLoud == null || mCount == 0)
                return false;

            lock (SoLoud.mAudioThreadMutex)
            {
                bool res = mSource[mReadIndex]!.Source == source;
                return res;
            }
        }

        /// <summary>
        /// Set params by reading them from the given audio source.
        /// </summary>
        public SoLoudStatus SetParamsFromAudioSource(AudioSource source)
        {
            mChannels = source.mChannels;
            mBaseSamplerate = source.mBaseSamplerate;

            return SoLoudStatus.Ok;
        }

        /// <summary>
        /// Set params manually.
        /// </summary>
        public SoLoudStatus SetParams(float sampleRate, uint channels = 2)
        {
            if (channels < 1 || channels > SoLoud.MaxChannels)
                return SoLoudStatus.InvalidParameter;

            mChannels = channels;
            mBaseSamplerate = sampleRate;
            return SoLoudStatus.Ok;
        }

        /// <summary>
        /// Find the channel the queue is playing on to calculate handle.
        /// </summary>
        internal Handle FindQueueHandle()
        {
            for (uint i = 0; mQueueHandle.Value == 0 && i < SoLoud.mHighestVoice; i++)
            {
                if (SoLoud.mVoice[i] == mInstance)
                {
                    mQueueHandle = SoLoud.getHandleFromVoice_internal(i);
                }
            }
            return mQueueHandle;
        }
    }
}
