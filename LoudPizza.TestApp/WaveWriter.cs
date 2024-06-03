using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace LoudPizza.TestApp
{
    public sealed class WaveWriter : IDisposable
    {
        private const string BLANK_HEADER = "RIFF\0\0\0\0WAVEfmt ";
        private const string BLANK_DATA_HEADER = "data\0\0\0\0";

        private BinaryWriter _writer;

        public WaveWriter(Stream stream, bool leaveOpen, int sampleRate, int channels)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen);

            // basic header
            _writer.Write(Encoding.UTF8.GetBytes(BLANK_HEADER));
            // fmt header size
            _writer.Write(18);
            // encoding (IeeeFloat)
            _writer.Write((short)3);
            // channels
            _writer.Write((short)channels);
            // samplerate
            _writer.Write(sampleRate);
            // averagebytespersecond
            int blockAlign = channels * sizeof(float);
            _writer.Write(blockAlign * sampleRate);
            // blockalign
            _writer.Write((short)blockAlign);
            // bitspersample (32)
            _writer.Write((short)32);
            // extrasize
            _writer.Write((short)0);
            // "data\0\0\0\0"
            _writer.Write(Encoding.UTF8.GetBytes(BLANK_DATA_HEADER));
        }

        public void WriteSamples(ReadOnlySpan<float> buf)
        {
            Span<byte> tmp = stackalloc byte[2048];

            while (buf.Length > 0)
            {
                int toRead = Math.Min(tmp.Length / sizeof(float), buf.Length);

                ReadOnlySpan<float> src = buf.Slice(0, toRead);
                Span<byte> dst = tmp.Slice(0, toRead * sizeof(float));

                for (int i = 0; i < src.Length; i++)
                {
                    BinaryPrimitives.WriteSingleLittleEndian(dst.Slice(i * sizeof(float), sizeof(float)), src[i]);
                }

                _writer.Write(dst);
                buf = buf.Slice(toRead);
            }
        }

        public void Dispose()
        {
            // RIFF chunk size
            _writer.Seek(4, SeekOrigin.Begin);
            _writer.Write((uint)(_writer.BaseStream.Length - 8));

            // data chunk size
            _writer.Seek(44, SeekOrigin.Begin);
            _writer.Write((uint)(_writer.BaseStream.Length - 48));

            _writer?.Dispose();
            _writer = null!;
        }
    }
}
