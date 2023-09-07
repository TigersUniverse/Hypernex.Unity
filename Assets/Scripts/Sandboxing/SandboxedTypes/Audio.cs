using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Audio
    {
        private static AudioSource GetAudioSource(Item item)
        {
            AudioSource a = item.t.GetComponent<AudioSource>();
            if (a == null)
                return null;
            return a;
        }

        public static bool IsPlaying(Item item) => GetAudioSource(item)?.isPlaying ?? false;
        public static bool IsMuted(Item item) => GetAudioSource(item)?.mute ?? false;
        public static bool IsLooping(Item item) => GetAudioSource(item)?.loop ?? false;
        public static void Play(Item item) => GetAudioSource(item)?.Play();
        public static void Pause(Item item) => GetAudioSource(item)?.Pause();
        public static void Resume(Item item) => GetAudioSource(item)?.UnPause();
        public static void Stop(Item item) => GetAudioSource(item)?.Stop();

        public static void SetAudioClip(Item item, string asset)
        {
            AudioSource audioSource = GetAudioSource(item);
            AudioClip audioClip = (AudioClip) SandboxTools.GetObjectFromWorldResource(asset);
            if(audioSource == null || audioClip == null)
                return;
            audioSource.clip = audioClip;
        }

        public static void SetMute(Item item, bool value)
        {
            AudioSource audioSource = GetAudioSource(item);
            if(audioSource == null)
                return;
            audioSource.mute = value;
        }
        
        public static void SetLoop(Item item, bool value)
        {
            AudioSource audioSource = GetAudioSource(item);
            if(audioSource == null)
                return;
            audioSource.loop = value;
        }
        
        public static float GetPitch(Item item) => GetAudioSource(item)?.pitch ?? 0.0f;
        public static void SetPitch(Item item, float value)
        {
            AudioSource audioSource = GetAudioSource(item);
            if(audioSource == null)
                return;
            audioSource.pitch = value;
        }
        
        public static float GetVolume(Item item) => GetAudioSource(item)?.volume ?? 0.0f;
        public static void SetVolume(Item item, float value)
        {
            AudioSource audioSource = GetAudioSource(item);
            if(audioSource == null)
                return;
            audioSource.volume = value;
        }
        
        public static float GetPosition(Item item) => GetAudioSource(item)?.time ?? 0.0f;
        public static void SetPosition(Item item, float value)
        {
            AudioSource audioSource = GetAudioSource(item);
            if(audioSource == null)
                return;
            audioSource.time = value;
        }

        public static float GetLength(Item item)
        {
            AudioSource audioSource = GetAudioSource(item);
            if(audioSource == null || audioSource.clip == null)
                return 0.0f;
            return audioSource.clip.length;
        }
    }
}