using System;
using System.Collections;
using System.Collections.Generic;
using Hypernex.Tools;
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
            audioCodecs.Add(new ConcentusAudioCodec());
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
            audioSource.clip = AudioClip.Create("", pcm.Length / channels, channels, frequency, false);
            audioSource.clip.SetData(pcm, 0);
            /*if(!audioSource.loop)
                audioSource.loop = true;*/
            if(!audioSource.isPlaying)
                audioSource.Play();
        }

        public static void AddQueue(AudioSource audioSource, float[] pcm, int channels, int frequency)
        {
            var buffer = audioSource.GetComponent<BufferAudioSource>();
            if (buffer == null)
                buffer = audioSource.gameObject.AddComponent<BufferAudioSource>();
            buffer.AddQueue(pcm, channels, frequency);
        }

        public static void InsertQueue(AudioSource audioSource, float[] pcm, int channels, int frequency, int index)
        {
            var buffer = audioSource.GetComponent<BufferAudioSource>();
            if (buffer == null)
                buffer = audioSource.gameObject.AddComponent<BufferAudioSource>();
            buffer.AddInsertQueue(pcm, channels, frequency, index);
        }

        public static void AddQueueLater(AudioSource audioSource, float[] pcm, int channels, int frequency, float delay)
        {
            CoroutineRunner.Instance.Run(AddQueueLaterInternal(audioSource, pcm, channels, frequency, delay));
        }

        private static IEnumerator AddQueueLaterInternal(AudioSource audioSource, float[] pcm, int channels, int frequency, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            var buffer = audioSource.GetComponent<BufferAudioSource>();
            if (buffer == null)
                buffer = audioSource.gameObject.AddComponent<BufferAudioSource>();
            buffer.AddQueue(pcm, channels, frequency);
        }
    }
}