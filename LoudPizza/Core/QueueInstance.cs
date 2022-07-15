using System;

namespace LoudPizza.Core
{
    public unsafe class QueueInstance : AudioSourceInstance
    {
        protected Queue mParent;

        public QueueInstance(Queue aParent)
        {
            mParent = aParent;
            mFlags |= Flags.Protected;
        }

        public override uint getAudio(Span<float> aBuffer, uint aSamplesToRead, uint aBufferSize)
        {
            Queue parent = mParent;
            if (parent.mCount == 0)
            {
                return 0;
            }

            uint copycount = aSamplesToRead;
            uint copyofs = 0;
            while (copycount != 0 && parent.mCount != 0)
            {
                AudioSourceInstance source = parent.mSource[parent.mReadIndex]!;
                uint readcount = source.getAudio(aBuffer.Slice((int)copyofs), copycount, aBufferSize);
                copyofs += readcount;
                copycount -= readcount;
                if (source.hasEnded())
                {
                    source.Dispose();
                    parent.mSource[parent.mReadIndex] = null;
                    parent.mReadIndex = (parent.mReadIndex + 1) % (uint)parent.mSource.Length;
                    parent.mCount--;
                    mLoopCount++;
                }
            }
            return copyofs;
        }

        public override unsafe SoLoudStatus seek(ulong aSamplePosition, Span<float> mScratch)
        {
            return SoLoudStatus.NotImplemented;
        }

        public override bool hasEnded()
        {
            return mLoopCount != 0 && mParent.mCount == 0;
        }
    }
}
