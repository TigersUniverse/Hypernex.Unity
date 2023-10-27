using Concentus.Enums;
using Concentus.Structs;
using Hypernex.Networking.Messages;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Game.Audio
{
    public class OpusAudioCodec : IAudioCodec
    {
        public string Name => "Opus";

        private static OpusEncoder encoder;

        internal static void MicrophoneOff() => encoder = null;
        
        public PlayerVoice Encode(float[] pcm, AudioClip clip, JoinAuth joinAuth)
        {
            encoder ??= new(Mic.Frequency, clip.channels, OpusApplication.OPUS_APPLICATION_AUDIO);
            encoder.Bitrate = 12000;
            byte[] outputBuffer = new byte[1275];
            int packetSize = encoder.Encode(pcm, 0, Mic.FRAME_SIZE, outputBuffer, 0, outputBuffer.Length);
            return new PlayerVoice
            {
                Auth = joinAuth,
                Bitrate = encoder.Bitrate,
                Channels = encoder.channels,
                EncodeLength = packetSize,
                Bytes = outputBuffer,
                Encoder = Name,
                SampleRate = Mic.Frequency
            };
        }

        public void Decode(PlayerVoice playerVoice, AudioSource audioSource)
        {
            OpusDecoder decoder = new OpusDecoder(playerVoice.SampleRate, playerVoice.Channels);
            byte[] compressedPacket = playerVoice.Bytes;
            int frameSize = OpusPacketInfo.GetNumSamples(decoder, compressedPacket, 0, compressedPacket.Length);
            float[] outputBuffer = new float[frameSize];
            decoder.Decode(compressedPacket, 0, compressedPacket.Length, outputBuffer, 0, frameSize);
            AudioSourceDriver.Set(audioSource, outputBuffer, playerVoice.Channels, playerVoice.SampleRate);
        }
    }
}