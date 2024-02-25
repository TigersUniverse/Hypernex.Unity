using System;
using System.Collections.Generic;
using Hypernex.Networking.Messages;
using Hypernex.Tools;
using UnityEngine;
using UnityOpus;

namespace Hypernex.Game.Audio
{
    public class OpusAudioCodec : IAudioCodec
    {
        public const float FrameSize = 20f;
        public const float MaxFrameSize = 120f;

        public string Name => "OpusNative";

        private static Encoder encoder;

        internal static void MicrophoneOff() => encoder = null;

        private Queue<float> queue = new Queue<float>();

        public PlayerVoice[] Encode(float[] pcm, AudioClip clip, JoinAuth joinAuth)
        {
            if (encoder == null)
            {
                encoder = new((SamplingFrequency)clip.frequency, (NumChannels)clip.channels, OpusApplication.VoIP);
                encoder.Bitrate = 61440;
                encoder.Complexity = 10;
                encoder.Signal = OpusSignal.Auto;
            }

            for (int i = 0; i < pcm.Length; i++)
            {
                queue.Enqueue(pcm[i]);
            }

            List<PlayerVoice> voicePackets = new List<PlayerVoice>();
            int frameLength = Mathf.RoundToInt(FrameSize / 1000f * clip.frequency * clip.channels);
            while (queue.Count >= frameLength)
            {
                float[] dataPcm = new float[frameLength];
                for (int i = 0; i < dataPcm.Length; i++)
                {
                    dataPcm[i] = queue.Dequeue();
                }
                byte[] outputEncodeBuffer = new byte[4000];
                int packetSize = encoder.Encode(dataPcm, outputEncodeBuffer);
                byte[] buf = new byte[packetSize];
                Array.Copy(outputEncodeBuffer, buf, buf.Length);
                PlayerVoice voice = new PlayerVoice
                {
                    Auth = joinAuth,
                    Bitrate = encoder.Bitrate,
                    Channels = clip.channels,
                    EncodeLength = frameLength,
                    Bytes = buf,
                    Encoder = Name,
                    SampleRate = clip.frequency
                };
                voicePackets.Add(voice);
            }
            return voicePackets.ToArray();
        }

        public void Decode(PlayerVoice playerVoice, AudioSource audioSource)
        {
            using Decoder decoder = new((SamplingFrequency)playerVoice.SampleRate, (NumChannels)playerVoice.Channels);
            byte[] compressedPacket = playerVoice.Bytes;
            int frameSize = playerVoice.EncodeLength;
            int frameLength = Mathf.RoundToInt(MaxFrameSize / 1000f * playerVoice.SampleRate);
            float[] outputBuffer = new float[frameLength];
            int length = decoder.Decode(compressedPacket, compressedPacket.Length, outputBuffer);
            float[] pcmBuffer = new float[length];
            // Debug.Assert(length == frameSize);
            Array.Copy(outputBuffer, pcmBuffer, pcmBuffer.Length);

            AudioSourceDriver.AddQueue(audioSource, pcmBuffer, playerVoice.Channels, playerVoice.SampleRate);
        }
    }
}