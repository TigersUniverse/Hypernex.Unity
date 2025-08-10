using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.Networking.SandboxedClasses;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Game.Video.StreamProviders
{
    public class YouTubeStreamProvider : IStreamProvider
    {
        private YoutubeClient client = new YoutubeClient();

        public static string VideoFormat { get; set; } = ".mp4";
        public static string AudioFormat { get; set; } = ".mp3";
        public static int VideoQuality { get; set; } = 720;
        
        public bool IsHostnameSupported(VideoRequest req)
        {
            try
            {
                Uri u = new Uri(req.GetMediaUrl());
                switch (u.Host)
                {
                    case "youtube.com":
                    case "www.youtube.com":
                    case "m.youtube.com":
                    case "youtu.be":
                        return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async void DownloadVideo(VideoRequest req, Action<string> callback)
        {
            try
            {
                string videoUrl = req.GetMediaUrl();
                YoutubeExplode.Videos.Video v = await client.Videos.GetAsync(videoUrl);
                string fileType = req.Options.AudioOnly ? AudioFormat : VideoFormat;
                string f = v.Title;
                SanitizePath(ref f);
                string fileName = Path.Combine(Init.Instance.GetMediaLocation(), f + fileType);
                StreamManifest streamManifest = await client.Videos.Streams.GetManifestAsync(videoUrl);
                List<IStreamInfo> streamInfos = new List<IStreamInfo>();
                MuxedStreamInfo[] muxed = streamManifest.GetMuxedStreams().ToArray();
#if !UNITY_IOS && !UNITY_ANDROID
            if (muxed.Length > 0)
            {
                streamInfos.Add(PickConditionOrDefault(muxed,
                    info => info.VideoQuality.Label.Contains(VideoQuality + "p"), GetClosestVideoQuality));
            }
            else
            {
                IVideoStreamInfo[] videos = streamManifest.GetVideoStreams().ToArray();
                streamInfos.Add(PickConditionOrDefault(videos,
                    info => info.VideoQuality.Label.Contains(VideoQuality + "p"), GetClosestVideoQuality));
                streamInfos.Add(streamManifest.GetAudioStreams().GetWithHighestBitrate());
            }
#else
                if (muxed.Length > 0)
                    streamInfos.Add(PickConditionOrDefault(muxed,
                        info => info.VideoQuality.Label.Contains(VideoQuality + "p"), GetClosestVideoQuality));
                else
                {
                    // TODO: Combine streams on mobile platforms
                    callback.Invoke(String.Empty);
                    return;
                }
#endif
                ConversionRequest c = new ConversionRequestBuilder(fileName).SetPreset(ConversionPreset.UltraFast)
                    .SetFFmpegPath(Init.Instance.FFMpegExecutable).Build();
                await client.Videos.DownloadAsync(streamInfos, c);
                callback.Invoke(fileName);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
                callback.Invoke(String.Empty);
            }
        }
        
        private static void SanitizePath(ref string path)
        {
            path = path.Replace(".", "_");
            path = path.Replace("/", "_");
            path = path.Replace("\\", "_");
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                path = path.Replace(invalid, '_');
            }
        }

        private static T PickConditionOrDefault<T>(T[] arr, Func<T, bool> match, Func<T[], T> def)
        {
            foreach (T t in arr)
            {
                if(!match.Invoke(t)) continue;
                return t;
            }
            return def.Invoke(arr);
        }

        private static T GetClosestVideoQuality<T>(T[] vs) where T : IVideoStreamInfo
        {
            IVideoStreamInfo closest = vs[0];
            int d = -1;
            foreach (T v in vs)
            {
                int q = Convert.ToInt32(v.VideoQuality.Label.Split("p")[0]);
                int diffTest = Math.Abs(q - VideoQuality);
                if (diffTest < d || d < 0)
                {
                    d = diffTest;
                    closest = v;
                }
            }
            return (T) closest;
        }
    }
}