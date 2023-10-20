using System;
using Concentus.Enums;
using Concentus.Structs;
using Hypernex.Networking.Messages;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Game.Audio
{
    public class OpusAudioCodec : IAudioCodec
    {
        private const int FRAME_SIZE = 960;
        
        public string Name => "Opus";

        private static OpusEncoder encoder;

        internal static void MicrophoneOff() => encoder = null;
        
        public PlayerVoice Encode(float[] pcm, AudioClip clip, JoinAuth joinAuth)
        {
            encoder ??= new(Mic.Frequency, clip.channels, OpusApplication.OPUS_APPLICATION_VOIP);
            encoder.Bitrate = 12000;
            byte[] output = new byte[1275];
            int packetSize = encoder.Encode(pcm, 0, FRAME_SIZE, output, 0, output.Length);
            return new PlayerVoice
            {
                Auth = joinAuth,
                Bitrate = encoder.Bitrate,
                Channels = encoder.channels,
                EncodeLength = packetSize,
                Bytes = output,
                Encoder = Name,
                SampleRate = Mic.Frequency
            };
        }

        public void Decode(PlayerVoice playerVoice, AudioSource audioSource)
        {
            OpusDecoder decoder = new OpusDecoder(playerVoice.SampleRate, playerVoice.Channels);
            float[] d = new float[playerVoice.EncodeLength];
            decoder.Decode(playerVoice.Bytes, 0, d.Length, d, 0, FRAME_SIZE);
            AudioSourceDriver.Set(audioSource, d, playerVoice.Channels, playerVoice.SampleRate);
        }
    }
}