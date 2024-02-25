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
        private uint PacketCounter = 0;

        public PlayerVoice[] Encode(float[] pcm, AudioClip clip, JoinAuth joinAuth)
        {
            if (pcm.Length == 0)
            {
                queue.Clear();
                return Array.Empty<PlayerVoice>();
            }

            if (encoder == null)
            {
                encoder = new(clip.frequency, clip.channels, OpusApplication.OPUS_APPLICATION_VOIP);
                encoder.Bitrate = 61440;
                encoder.Complexity = 10;
                encoder.SignalType = OpusSignal.OPUS_SIGNAL_AUTO;
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
                byte[] buf = new byte[packetSize + sizeof(uint)];
                BitConverter.TryWriteBytes(buf, PacketCounter);
                Array.Copy(outputEncodeBuffer, 0, buf, sizeof(uint), Math.Min(buf.Length - sizeof(uint), outputEncodeBuffer.Length));
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
                PacketCounter++;
            }
            // Debug.Log(voicePackets.Count);
            return voicePackets.ToArray();
        }

        public void Decode(PlayerVoice playerVoice, AudioSource audioSource)
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
            uint packetIndex = BitConverter.ToUInt32(compressedPacket, 0);
            int frameSize = playerVoice.EncodeLength;
            int frameLength = Mathf.RoundToInt(MaxFrameSize / 1000f * decoder.SampleRate * decoder.NumChannels);
            float[] outputBuffer = new float[frameLength];
            int length = decoder.Decode(compressedPacket, sizeof(uint), compressedPacket.Length - sizeof(uint), outputBuffer, 0, outputBuffer.Length / decoder.NumChannels);
            float[] pcmBuffer = new float[length];
            // Debug.Assert(length == frameSize);
            Array.Copy(outputBuffer, pcmBuffer, pcmBuffer.Length);

            AudioSourceDriver.AddQueue(audioSource, pcmBuffer, decoder.NumChannels, decoder.SampleRate);
        }
    }
}