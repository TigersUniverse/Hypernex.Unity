using System.IO;
using UnityEngine;
using UnityEngine.Video;

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
        
        private static VideoPlayer GetVideoPlayer(Item item)
        {
            VideoPlayer v = item.t.GetComponent<VideoPlayer>();
            if (v == null)
                return null;
            return v;
        }

        public static bool IsValid(Item item) => GetVideoPlayer(item) != null;
        
        public static bool IsPlaying(Item item) => GetVideoPlayer(item)?.isPlaying ?? false;
        public static bool IsMuted(Item item) => GetAudioSource(item)?.mute ?? false;
        public static bool IsLooping(Item item) => GetVideoPlayer(item)?.isLooping ?? false;
        public static void Play(Item item) => GetVideoPlayer(item)?.Play();
        public static void Pause(Item item) => GetVideoPlayer(item)?.Pause();
        public static void Stop(Item item) => GetVideoPlayer(item)?.Stop();
        
        public static void SetVideoClip(Item item, string asset)
        {
            VideoPlayer videoPlayer = GetVideoPlayer(item);
            VideoClip videoClip = (VideoClip) SandboxTools.GetObjectFromWorldResource(asset);
            if(videoPlayer == null || videoClip == null)
                return;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
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
            VideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            videoPlayer.isLooping = value;
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
        
        public static double GetPosition(Item item) => GetVideoPlayer(item)?.time ?? 0.0;
        public static void SetPosition(Item item, float value)
        {
            VideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            videoPlayer.time = value;
        }

        public static double GetLength(Item item)
        {
            VideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return 0.0;
            return videoPlayer.length;
        }

        public static void LoadFromCobalt(Item item, CobaltDownload cobaltDownload)
        {
            VideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            if (!File.Exists(cobaltDownload.PathToFile))
                return;
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = "file://" + cobaltDownload.PathToFile;
        }
    }
}