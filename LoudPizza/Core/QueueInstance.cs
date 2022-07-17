using System;
using System.Runtime.CompilerServices;

namespace LoudPizza.Core
{
    public unsafe class QueueInstance : AudioSourceInstance
    {
        public new Queue Source => Unsafe.As<Queue>(base.Source);

        public QueueInstance(Queue source) : base(source)
        {
            mFlags |= Flags.Protected;
        }

        public override uint GetAudio(Span<float> buffer, uint samplesToRead, uint bufferSize)
        {
            Queue parent = Source;
            if (parent.mCount == 0)
            {
                return 0;
            }

            uint copycount = samplesToRead;
            uint copyofs = 0;
            while (copycount != 0 && parent.mCount != 0)
            {
                AudioSourceInstance source = parent.mSource[parent.mReadIndex]!;
                uint readcount = source.GetAudio(buffer.Slice((int)copyofs), copycount, bufferSize);
                copyofs += readcount;
                copycount -= readcount;
                if (source.HasEnded())
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

        public override unsafe SoLoudStatus Seek(ulong aSamplePosition, Span<float> mScratch)
        {
            return SoLoudStatus.NotImplemented;
        }

        public override bool HasEnded()
        {
            return mLoopCount != 0 && Source.mCount == 0;
        }
    }
}
