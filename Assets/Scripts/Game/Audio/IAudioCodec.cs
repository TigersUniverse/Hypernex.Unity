using Hypernex.Networking.Messages;
using UnityEngine;

namespace Hypernex.Game.Audio
{
    public interface IAudioCodec
    {
        public string Name { get; }
        public PlayerVoice[] Encode(float[] pcm, AudioClip clip, JoinAuth joinAuth);
        public void Decode(PlayerVoice playerVoice, AudioSource audioSource);
    }
}