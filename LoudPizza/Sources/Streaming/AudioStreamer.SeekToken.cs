using System;
using System.Threading;

namespace LoudPizza.Sources.Streaming
{
    public partial class AudioStreamer
    {
        internal class SeekToken
        {
            public readonly ManualResetEventSlim WaitHandle;

            public ulong TargetPosition;
            public AudioSeekFlags Flags;

            public ulong ResultPosition;
            public SoLoudStatus ResultStatus;
            public Exception? Exception;

            public SeekToken()
            {
                WaitHandle = new ManualResetEventSlim();
            }
        }
    }
}
