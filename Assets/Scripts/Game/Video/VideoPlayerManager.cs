using System;
using System.Collections.Generic;
using System.IO;
using Hypernex.CCK.Unity.Internals;

namespace Hypernex.Game.Video
{
    public static class VideoPlayerManager
    {
        public static Type DefaultVideoPlayerType
        {
            get => defaultVideoPlayerType;
            set
            {
                if(value == null) return;
                if(value is not IVideoPlayer) return;
                defaultVideoPlayerType = value;
            }
        }

        private static Type defaultVideoPlayerType = typeof(UnityVideoPlayer);
        private static Dictionary<Type, Func<Uri, bool>> videoPlayerTypes = new();

        static VideoPlayerManager()
        {
            // Register Built-In VideoPlayers
            Register<UnityVideoPlayer>(UnityVideoPlayer.CanBeUsed, UnityVideoPlayer.CanBeUsed);
#if VLC
            Register<VLCVideoPlayer>(VLCVideoPlayer.CanBeUsed, VLCVideoPlayer.CanBeUsed);
#endif
        }

        public static void Register<T>(Func<bool> canBeUsed, Func<Uri, bool> fileCanBePlayed) where T : IVideoPlayer
        {
            if(!canBeUsed.Invoke()) return;
            videoPlayerTypes.Add(typeof(T), fileCanBePlayed);
        }

#nullable enable
        public static Type? GetVideoPlayerType(Uri uri)
        {
            foreach (KeyValuePair<Type,Func<Uri,bool>> keyValuePair in videoPlayerTypes)
            {
                if (!keyValuePair.Value.Invoke(uri)) continue;
                return keyValuePair.Key;
            }
            return DefaultVideoPlayerType;
        }
#nullable restore

        public static bool IsStream(Uri uri)
        {
            bool isStream = false;
            switch (uri.Scheme.ToLower())
            {
                case "rtmp":
                case "rtsp":
                case "srt":
                case "udp":
                case "tcp":
                    isStream = true;
                    break;
            }
            if (isStream) return true;
            string fileName = Path.GetFileName(uri.LocalPath);
            string ext = Path.GetExtension(fileName);
            switch (ext)
            {
                case ".m3u8":
                case ".mpd":
                case ".flv":
                    isStream = true;
                    break;
            }
            return isStream;
        }
    }
}