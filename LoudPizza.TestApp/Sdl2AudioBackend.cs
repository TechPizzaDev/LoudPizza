using System;
using System.Diagnostics;
using LoudPizza.Core;
using static SharpInterop.SDL2.SDL;

namespace LoudPizza.TestApp
{
    public unsafe class Sdl2AudioBackend
    {
        private Stopwatch watch = new();
        private int loops;

        public SDL_AudioSpec gActiveAudioSpec;
        public uint gAudioDeviceID;

        private SDL_AudioCallback audioCallback;

        public SoLoud SoLoud { get; }

        public Sdl2AudioBackend(SoLoud soloud)
        {
            SoLoud = soloud ?? throw new ArgumentNullException(nameof(soloud));
        }

        public SoLoudStatus Initialize(uint sampleRate = 48000, uint bufferSize = 512, uint channels = 0)
        {
            //if (!SDL_WasInit(SDL_INIT_AUDIO))
            //{
            //    if (SDL_InitSubSystem(SDL_INIT_AUDIO) < 0)
            //    {
            //        return SOLOUD_ERRORS.UNKNOWN_ERROR;
            //    }
            //}

            audioCallback = soloud_sdl2static_audiomixer;

            SDL_AudioSpec spec;
            spec.silence = default;
            spec.userdata = default;
            spec.size = default;
            spec.callback = audioCallback;

            spec.freq = (int)sampleRate;
            spec.format = AUDIO_F32;
            spec.channels = (byte)channels;
            spec.samples = (ushort)bufferSize;

            int flags = (int)(SDL_AUDIO_ALLOW_ANY_CHANGE & (~SDL_AUDIO_ALLOW_FORMAT_CHANGE));

            gAudioDeviceID = SDL_OpenAudioDevice(IntPtr.Zero, 0, ref spec, out SDL_AudioSpec activeSpec, flags);
            if (gAudioDeviceID == 0)
            {
                spec.format = AUDIO_S16;

                gAudioDeviceID = SDL_OpenAudioDevice(IntPtr.Zero, 0, ref spec, out activeSpec, flags);
            }

            if (gAudioDeviceID == 0)
            {
                return SoLoudStatus.UnknownError;
            }

            SoLoud.postinit_internal((uint)activeSpec.freq, activeSpec.samples, activeSpec.channels);
            gActiveAudioSpec = activeSpec;

            SoLoud.mBackendCleanupFunc = soloud_sdl2_deinit;
            SoLoud.mBackendString = "SDL2";

            SDL_PauseAudioDevice(gAudioDeviceID, 0); // start playback

            return SoLoudStatus.Ok;
        }

        private void soloud_sdl2static_audiomixer(IntPtr userdata, IntPtr stream, int length)
        {
            watch.Start();
            if (gActiveAudioSpec.format == AUDIO_F32)
            {
                int samples = length / (gActiveAudioSpec.channels * sizeof(float));
                SoLoud.mix((float*)stream, (uint)samples);
            }
            else
            {
                int samples = length / (gActiveAudioSpec.channels * sizeof(short));
                SoLoud.mixSigned16((short*)stream, (uint)samples);
            }
            watch.Stop();

            loops++;
            if (loops >= 48000 / 512)
            {
                Console.WriteLine("Mixing time: " + watch.Elapsed.TotalMilliseconds + "ms");
                watch.Reset();
                loops = 0;
            }
        }

        private void soloud_sdl2_deinit(SoLoud aSoloud)
        {
            SDL_CloseAudioDevice(gAudioDeviceID);
        }
    }
}