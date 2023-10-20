using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.Game.Audio
{
    public static class AudioSourceDriver
    {
        public static List<IAudioCodec> AudioCodecs => new (audioCodecs);
        private static readonly List<IAudioCodec> audioCodecs = new ();

        static AudioSourceDriver() => Refresh();

        internal static void Refresh()
        {
            audioCodecs.Clear();
            audioCodecs.Add(new RawAudioCodec());
            audioCodecs.Add(new OpusAudioCodec());
        }

        public static IAudioCodec GetAudioCodecByName(string name)
        {
            foreach (IAudioCodec audioCodec in AudioCodecs)
            {
                if (string.Equals(audioCodec.Name, name, StringComparison.CurrentCultureIgnoreCase))
                    return audioCodec;
            }
            return null;
        }
        
        public static void Set(AudioSource audioSource, float[] pcm, int channels, int frequency)
        {
            audioSource.clip = AudioClip.Create("", pcm.Length, channels, frequency, false);
            audioSource.clip.SetData(pcm, 0);
            /*if(!audioSource.loop)
                audioSource.loop = true;*/
            if(!audioSource.isPlaying)
                audioSource.Play();
        }
    }
}