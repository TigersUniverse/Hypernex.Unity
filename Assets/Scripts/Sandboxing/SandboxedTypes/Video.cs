using System.IO;
using Hypernex.CCK.Unity;
using Hypernex.Game.Video;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Video
    {
        private static AudioSource GetAudioSource(Item item)
        {
            AudioSource a = item.t.GetComponent<AudioSource>();
            if (a == null)
                return null;
            return a;
        }
        
        private static IVideoPlayer GetVideoPlayer(Item item)
        {
            VideoPlayerDescriptor v = item.t.GetComponent<VideoPlayerDescriptor>();
            if (v == null) return null;
            // TODO: There's only one supported video player
            if(v.CurrentVideoPlayer == null) v.Replace(typeof(UnityVideoPlayer));
            return v.CurrentVideoPlayer;
        }

        public static bool IsValid(Item item) => GetVideoPlayer(item) != null;
        
        public static bool IsPlaying(Item item) => GetVideoPlayer(item)?.IsPlaying ?? false;
        public static bool IsMuted(Item item) => GetAudioSource(item)?.mute ?? false;
        public static bool IsLooping(Item item) => GetVideoPlayer(item)?.Looping ?? false;
        public static void Play(Item item) => GetVideoPlayer(item)?.Play();
        public static void Pause(Item item) => GetVideoPlayer(item)?.Pause();
        public static void Stop(Item item) => GetVideoPlayer(item)?.Stop();
        
        public static void SetMute(Item item, bool value)
        {
            AudioSource audioSource = GetAudioSource(item);
            if(audioSource == null)
                return;
            audioSource.mute = value;
        }
        
        public static void SetLoop(Item item, bool value)
        {
            IVideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            videoPlayer.Looping = value;
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
        
        public static double GetPosition(Item item) => GetVideoPlayer(item)?.Position ?? 0.0;
        public static void SetPosition(Item item, float value)
        {
            IVideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            videoPlayer.Position = value;
        }

        public static double GetLength(Item item)
        {
            IVideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return 0.0;
            return videoPlayer.Length;
        }

        public static void LoadFromCobalt(Item item, CobaltDownload cobaltDownload)
        {
            // TODO: Switch Video Player type if needed (when they're implemented)
            IVideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            if (!File.Exists(cobaltDownload.PathToFile))
                return;
            videoPlayer.Source = "file://" + cobaltDownload.PathToFile;
        }
    }
}