using System;
using System.Collections.Generic;
using System.IO;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game.Video.StreamProviders;

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

        public static IReadOnlyList<IStreamProvider> StreamProviders = new[]
        {
            new YouTubeStreamProvider()
        };

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

        public static bool CanGetCodecs()
        {
#if VLC
            return true;
#endif
            return false;
        }

        public static string[] GetCodecs(Uri source)
        {
            List<string> codecs = new List<string>();
#if VLC
            // Yes, I know this is messy, but you have to admit, this is a
            // great way to not have a million different FFmpegs
            using LibVLCSharp.Media media = new LibVLCSharp.Media(source);
            if(VLCVideoPlayer.libVLC == null) VLCVideoPlayer.CreateLibVLC(Init.Instance.DebugVLC);
            LibVLCSharp.MediaParsedStatus stat = media.ParseAsync(VLCVideoPlayer.libVLC!).Result;
            if (stat == LibVLCSharp.MediaParsedStatus.Done)
            {
#nullable enable
                LibVLCSharp.MediaTrackList? videoTracks = media.TrackList(LibVLCSharp.TrackType.Video);
                if(videoTracks != null)
                    for (int i = 0; i < videoTracks.Count; i++)
                    {
                        var videoTrack = videoTracks[(uint) i];
                        if(videoTrack == null) continue;
                        string c = VLCVideoPlayer.FourCCToString(videoTrack.Codec).ToLowerInvariant();
                        codecs.Add(c);
                    }
#nullable restore
            }
            return codecs.ToArray();
#else
            return Array.Empty<string>();
#endif
        }

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