using Hypernex.Networking.Messages;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Game.Audio
{
    public class RawAudioCodec : IAudioCodec
    {
        public string Name => "raw";
        
        public PlayerVoice Encode(float[] pcm, AudioClip clip, JoinAuth joinAuth)
        {
            byte[] data = DataConversion.ConvertFloatToByte(pcm);
            PlayerVoice playerVoice = new PlayerVoice
            {
                Auth = joinAuth,
                Bitrate = 0,
                SampleRate = (int) Mic.Frequency,
                Channels = (int) Mic.NumChannels,
                Encoder = Name,
                Bytes = data,
                EncodeLength = data.Length
            };
            return playerVoice;
        }

        public void Decode(PlayerVoice playerVoice, AudioSource audioSource)
        {
            float[] d = DataConversion.ConvertByteToFloat(playerVoice.Bytes);
            AudioSourceDriver.Set(audioSource, d, playerVoice.Channels, playerVoice.SampleRate);
        }
    }
}