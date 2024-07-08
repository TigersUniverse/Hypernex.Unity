using System;
using System.IO;
using Hypernex.CCK.Unity;
using Hypernex.Game.Video;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Video
    {
        private static VideoPlayerDescriptor GetVideoPlayerDescriptor(Item item)
        {
            VideoPlayerDescriptor v = item.t.GetComponent<VideoPlayerDescriptor>();
            return v;
        }
        
        private static IVideoPlayer GetVideoPlayer(Item item)
        {
            VideoPlayerDescriptor v = item.t.GetComponent<VideoPlayerDescriptor>();
            if (v == null) return null;
            if (v.CurrentVideoPlayer == null) return null;
            return v.CurrentVideoPlayer;
        }

        public static bool IsValid(Item item) => GetVideoPlayer(item) != null;
        
        public static bool IsPlaying(Item item) => GetVideoPlayer(item)?.IsPlaying ?? false;
        public static bool IsMuted(Item item) => GetVideoPlayer(item)?.Muted ?? false;
        public static bool IsLooping(Item item) => GetVideoPlayer(item)?.Looping ?? false;
        public static void Play(Item item) => GetVideoPlayer(item)?.Play();
        public static void Pause(Item item) => GetVideoPlayer(item)?.Pause();
        public static void Stop(Item item) => GetVideoPlayer(item)?.Stop();
        
        public static void SetMute(Item item, bool value)
        {
            IVideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            videoPlayer.Muted = value;
        }
        
        public static void SetLoop(Item item, bool value)
        {
            IVideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            videoPlayer.Looping = value;
        }
        
        public static float GetPitch(Item item) => GetVideoPlayer(item)?.Pitch ?? 0.0f;
        public static void SetPitch(Item item, float value)
        {
            IVideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            videoPlayer.Pitch = value;
        }
        
        public static float GetVolume(Item item) => GetVideoPlayer(item)?.Volume ?? 0.0f;

        public static void SetVolume(Item item, float value)
        {
            IVideoPlayer videoPlayer = GetVideoPlayer(item);
            if(videoPlayer == null)
                return;
            videoPlayer.Volume = value;
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
            VideoPlayerDescriptor videoPlayerDescriptor = GetVideoPlayerDescriptor(item);
            if(videoPlayerDescriptor == null)
                return;
            if (cobaltDownload.isStream)
            {
                IVideoPlayer videoPlayer = videoPlayerDescriptor.Replace(
                    VideoPlayerManager.GetVideoPlayerType(new Uri(cobaltDownload.PathToFile)) ??
                    VideoPlayerManager.DefaultVideoPlayerType);
                if (videoPlayer == null)
                    return;
                videoPlayer.Source = cobaltDownload.PathToFile;
            }
            else
            {
                if (!File.Exists(cobaltDownload.PathToFile))
                    return;
                string filePath = "file:///" + cobaltDownload.PathToFile;
                IVideoPlayer videoPlayer = videoPlayerDescriptor.Replace(
                    VideoPlayerManager.GetVideoPlayerType(new Uri(filePath)) ??
                    VideoPlayerManager.DefaultVideoPlayerType);
                if (videoPlayer == null)
                    return;
                videoPlayer.Source = filePath;
            }
        }
    }
}