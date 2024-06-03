using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LoudPizza.Core;
using LoudPizza.Modifiers;
using LoudPizza.Sources;
using LoudPizza.Sources.Streaming;
using LoudPizza.Vorbis;
using SharpInterop.SDL2;

namespace LoudPizza.TestApp
{
    internal unsafe class Program
    {
        public static void Main(string[] args)
        {
            SDL.SDL_Init(SDL.SDL_INIT_AUDIO);

            SdlAudioUtil? audioUtil = new(isCapture: false);

            SoLoud soLoud = new();

            AudioStreamer streamer = new();
            streamer.Start();

            int sampleRate = 48000;
            int channels = 2;
            int bufferSize = 1024;
            bool writeToFile = false;

            Sdl2AudioBackend? backend = null;
            if (!writeToFile)
            {
                backend = new Sdl2AudioBackend(soLoud);
                backend.Initialize((uint)sampleRate, (uint)bufferSize);
            }
            else
            {
                soLoud.postinit_internal((uint)sampleRate, (uint)bufferSize, (uint)channels);
            }

            SoLoudHandle so = new(soLoud);
            so.SetMaxActiveVoiceCount(16);

            NVorbis.VorbisReader reader = new("test1.ogg");

            // HttpClient http = new();
            // var request = new HttpRequestMessage(HttpMethod.Get, "");
            // Console.WriteLine("Sending request");
            // var response = http.Send(request, HttpCompletionOption.ResponseHeadersRead);
            // Console.WriteLine("Opening stream");
            // var responseStream = response.Content.ReadAsStream();
            // NVorbis.VorbisReader reader = new(responseStream, false);
            // Console.WriteLine("Initializing stream");
            reader.Initialize();
            // Console.WriteLine("Stream initialized");

            VorbisAudioStream vorbisStream = new(reader);
            StreamedAudioStream streamedAudio = new(streamer, vorbisStream);
            streamer.RegisterStream(streamedAudio);

            AudioStream audioStream = new(soLoud, streamedAudio);

            byte[] file1 = File.ReadAllBytes("test1.raw");
            AudioBuffer buf = new(soLoud);
            float[] floats = MemoryMarshal.Cast<byte, float>(file1).ToArray();
            uint fileChannels = 1;
            if (fileChannels == 2)
            {
                float[] newFloats = new float[floats.Length];
                int center = floats.Length / 2;
                for (int i = 0; i < center; i++)
                {
                    newFloats[i + center * 0] = floats[i * 2 + 0];
                    newFloats[i + center * 1] = floats[i * 2 + 1];
                }
                floats = newFloats;
            }
            buf.LoadRawWave(floats, 44100, fileChannels);

            for (int i = 0; i < 0; i++)
            {
                VoiceHandle h = so.Play(buf, paused: false);
                h.IsLooping = false;
            }

            VoiceHandle asHandle = default;
            if (!writeToFile)
            {
                asHandle = so.Play(audioStream);
                asHandle.IsProtected = true;
                asHandle.IsLooping = true;
                asHandle.RelativePlaySpeed = 1.0f;
                asHandle.Volume = 0.5f;
                //asHandle.StreamSamplePosition = 1657800;
                //asHandle.StreamSamplePosition = 4053600;
            }

            if (writeToFile)
            {
                using WaveWriter writer = new(new FileStream("output.wav", FileMode.Create), false, sampleRate, channels);

                float[] buffer = new float[bufferSize * channels];
                short[] buffer16 = new short[bufferSize * channels];

                Stopwatch w = new Stopwatch();
                w.Start();

                VoiceHandle group = so.CreateVoiceGroup();

                int loops = (int)Math.Ceiling((sampleRate / (float)bufferSize) * 10);
                for (int i = 0; i < loops; i++)
                {
                    //if (i == 5)
                    //{
                    //    h.IsPaused = false;
                    //}
                    //
                    //if (i == 10)
                    //{
                    //    h.IsPaused = true;
                    //}
                    //
                    //if (i == 15)
                    //{
                    //    h.IsPaused = false;
                    //    h.SchedulePause(2);
                    //}

                    //if (i % 2 == 0)
                    //{
                    //    VoiceHandle h = so.Play(buf, paused: false);
                    //    h.IsLooping = false;
                    //    h.Volume = 0.2f;
                    //    group.AddVoiceToGroup(h);
                    //}

                    fixed (float* bufferPtr = buffer)
                    {
                        soLoud.mix(bufferPtr, (uint)bufferSize);
                    }

                    //soLoud.mixSigned16(buffer16, (uint)bufferSize);

                    //for (int j = 0; j < bufferSize * channels; j++)
                    //{
                    //    buffer[j] = buffer16[j] / (float)0x7fff;
                    //}

                    writer.WriteSamples(new ReadOnlySpan<float>(buffer, 0, bufferSize * channels));
                }

                w.Stop();

                Console.WriteLine($"Mixing finished in {w.Elapsed.TotalMilliseconds:0.0}ms");
            }

            AudioResampler[] resamplers = new AudioResampler[]
            {
                LinearAudioResampler.Instance,
                PointAudioResampler.Instance,
                CatmullRomAudioResampler.Instance,
            };
            int resamplerIndex = 0;

            Console.WriteLine("space = skip 1s");
            Console.WriteLine("left arrow = pitch down");
            Console.WriteLine("right arrow = pitch up");
            Console.WriteLine("up arrow = volume up");
            Console.WriteLine("down arrow = volume down");
            Console.WriteLine("R = cycle resampler");
            Console.WriteLine();

            while (true)
            {
                var key = Console.ReadKey().Key;
                if (key == ConsoleKey.Spacebar)
                {
                    asHandle.StreamSamplePosition += 48000;
                    Console.Write($"Skipped 1s (to {asHandle.StreamSamplePosition})");
                }
                else if (key == ConsoleKey.RightArrow)
                {
                    asHandle.RelativePlaySpeed = asHandle.RelativePlaySpeed + 0.025f;
                    Console.Write($"Increased pitch to {asHandle.RelativePlaySpeed:0.00}");
                }
                else if (key == ConsoleKey.LeftArrow)
                {
                    asHandle.RelativePlaySpeed = Math.Max(asHandle.RelativePlaySpeed - 0.025f, 0.025f);
                    Console.Write($"Decreased pitch to {asHandle.RelativePlaySpeed:0.00}");
                }
                else if (key == ConsoleKey.UpArrow)
                {
                    asHandle.Volume = Math.Min(asHandle.Volume + 0.01f, 1);
                    Console.Write($"Increased volume to {asHandle.Volume:0.00}");
                }
                else if (key == ConsoleKey.DownArrow)
                {
                    asHandle.Volume = Math.Max(asHandle.Volume - 0.01f, 0f);
                    Console.Write($"Decreased volume to {asHandle.Volume:0.00}");
                }
                else if (key == ConsoleKey.R)
                {
                    so.SetResampler(resamplers[resamplerIndex]);
                    Console.Write($"esampler set to {so.GetResampler().GetType().Name}");
                    resamplerIndex = (resamplerIndex + 1) % resamplers.Length;
                }
                else if (key == ConsoleKey.P)
                {
                    VoiceHandle h = so.Play(buf, paused: false);
                    h.IsLooping = false;
                    h.Volume = 0.2f;
                }
                Console.WriteLine();
            }
        }
    }
}