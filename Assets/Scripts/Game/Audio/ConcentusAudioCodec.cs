using System;
using System.Collections.Generic;
using System.Linq;
using Concentus.Enums;
using Concentus.Structs;
using Hypernex.Networking.Messages;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Game.Audio
{
    public class ConcentusAudioCodec : IAudioCodec
    {
        public class PlaybackInstance
        {
            public OpusDecoder decoder;
            public AudioSource source;
        }

        public const float FrameSize = 20f;
        public const float MaxFrameSize = 120f;

        public string Name => "Opus";

        private static OpusEncoder encoder;
        private static List<PlaybackInstance> decoders = new List<PlaybackInstance>();

        internal static void MicrophoneOff() => encoder = null;

        private Queue<float> queue = new Queue<float>();
        private int PacketCounterSamples = 0;
        
        private float[] Resample(float[] input, int inputRate, int outputRate, int channels)
        {
            if (inputRate == outputRate)
                return input;
            float ratio = (float) outputRate / inputRate;
            int inputSamples = input.Length / channels;
            int outputSamples = Mathf.RoundToInt(inputSamples * ratio);
            float[] output = new float[outputSamples * channels];
            for (int i = 0; i < outputSamples; i++)
            {
                float srcIndex = i / ratio;
                int index = (int)srcIndex;
                float frac = srcIndex - index;
                for (int c = 0; c < channels; c++)
                {
                    float a = input[Mathf.Min(index * channels + c, input.Length - 1)];
                    float b = input[Mathf.Min((index + 1) * channels + c, input.Length - 1)];
                    output[i * channels + c] = Mathf.Lerp(a, b, frac);
                }
            }
            return output;
        }

        public PlayerVoice[] Encode(float[] pcm, AudioClip clip, JoinAuth joinAuth)
        {
            if (pcm.Length == 0)
            {
                PacketCounterSamples = 0;
                queue.Clear();
                return Array.Empty<PlayerVoice>();
            }

            if (encoder == null)
            {
                PacketCounterSamples = 0;
                encoder = new(Mic.REQUESTED_FREQUENCY, clip.channels, OpusApplication.OPUS_APPLICATION_VOIP);
                encoder.Bitrate = 61440;
                encoder.Complexity = 10;
                encoder.SignalType = OpusSignal.OPUS_SIGNAL_AUTO;
            }
            
            if (Mic.REQUESTED_FREQUENCY != Mic.Frequency)
            {
                // Resample to Requested
                pcm = Resample(pcm, Mic.Frequency, Mic.REQUESTED_FREQUENCY, encoder.NumChannels);
            }

            for (int i = 0; i < pcm.Length; i++)
            {
                queue.Enqueue(pcm[i]);
            }

            List<PlayerVoice> voicePackets = new List<PlayerVoice>();
            int frameLength = Mathf.RoundToInt(FrameSize / 1000f * encoder.SampleRate * encoder.NumChannels);
            while (queue.Count >= frameLength)
            {
                float[] dataPcm = new float[frameLength];
                for (int i = 0; i < Math.Min(dataPcm.Length, queue.Count); i++)
                {
                    dataPcm[i] = queue.Dequeue();
                }
                byte[] outputEncodeBuffer = new byte[frameLength * sizeof(float)];
                int packetSize = encoder.Encode(dataPcm, 0, frameLength / encoder.NumChannels, outputEncodeBuffer, 0, outputEncodeBuffer.Length);
                byte[] buf = new byte[packetSize + sizeof(int)];
                BitConverter.TryWriteBytes(buf, PacketCounterSamples);
                Array.Copy(outputEncodeBuffer, 0, buf, sizeof(int), Math.Min(buf.Length - sizeof(int), outputEncodeBuffer.Length));
                // Array.Copy(outputEncodeBuffer, buf, Math.Min(buf.Length, outputEncodeBuffer.Length));
                PlayerVoice voice = new PlayerVoice
                {
                    Auth = joinAuth,
                    Bitrate = encoder.Bitrate,
                    Channels = encoder.channels,
                    EncodeLength = frameLength,
                    Bytes = buf,
                    Encoder = Name,
                    SampleRate = encoder.SampleRate
                };
                voicePackets.Add(voice);
                PacketCounterSamples += frameLength / encoder.NumChannels;
            }
            // Debug.Log(voicePackets.Count);
            return voicePackets.ToArray();
        }

        public float[] Decode(PlayerVoice playerVoice, AudioSource audioSource)
        {
            decoders.RemoveAll(x => x.source == null);
            var playback = decoders.FirstOrDefault(x => x.source == audioSource);
            if (playback == null)
            {
                playback = new PlaybackInstance();
                playback.source = audioSource;
                decoders.Add(playback);
            }
            playback.decoder ??= new OpusDecoder(playerVoice.SampleRate, playerVoice.Channels);
            OpusDecoder decoder = playback.decoder;

            byte[] compressedPacket = playerVoice.Bytes;
            int frameLength = Mathf.RoundToInt(FrameSize / 1000f * decoder.SampleRate * decoder.NumChannels);
            float[] outputBuffer = new float[frameLength];
            int length = decoder.Decode(compressedPacket, sizeof(int), compressedPacket.Length - sizeof(int), outputBuffer, 0, outputBuffer.Length / decoder.NumChannels);
            float[] pcmBuffer = new float[length * decoder.NumChannels];
            // Debug.Assert(length == frameSize);
            Array.Copy(outputBuffer, pcmBuffer, pcmBuffer.Length);
            int outputRate = AudioSettings.outputSampleRate;
            if (decoder.SampleRate != outputRate)
            {
                pcmBuffer = Resample(pcmBuffer, decoder.SampleRate, outputRate, decoder.NumChannels);
            }
            AudioSourceDriver.AddQueue(audioSource, pcmBuffer, decoder.NumChannels, outputRate);
            return pcmBuffer;
        }
    }
}