using System;
using System.IO;
using System.Threading;
using Hypernex.Networking.SandboxedClasses;
using Hypernex.Tools;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Game.Video.StreamProviders
{
    public class YouTubeStreamProvider : IStreamProvider
    {
        internal static YoutubeDL ytdl = new();
        
        private bool TryGetCookies(out string file)
        {
            string cookiesFile = Path.Combine(Init.Instance.GetPrivateLocation(), "cookies.txt");
            if (!File.Exists(cookiesFile))
            {
                file = null;
                return false;
            }
            file = cookiesFile;
            return true;
        }
        
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

        public async void DownloadVideo(VideoRequest req, Action<string, bool> callback)
        {
            // ytdlp is not supported on mobile platforms
#if UNITY_IOS || UNITY_ANDROID
            callback.Invoke(String.Empty, false);
            return;
#endif
            try
            {
                string url = req.GetMediaUrl();
                RunResult<VideoData> metaResult = await ytdl.RunVideoDataFetch(url);
                string liveUrl = String.Empty;
                if (metaResult.Success)
                {
                    switch (metaResult.Data.LiveStatus)
                    {
                        case LiveStatus.IsLive:
                            liveUrl = metaResult.Data.Url;
                            break;
                        case LiveStatus.IsUpcoming:
                            throw new Exception("Invalid LiveStream!");
                    }
                }
                if (!string.IsNullOrEmpty(liveUrl))
                {
                    callback.Invoke(liveUrl, true);
                    return;
                }
                OptionSet optionSet = new OptionSet
                {            
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_MAC
                    Format = "bestvideo[vcodec=vp8]/bestvideo[vcodec=h264]/bestvideo[vcodec*=avc1]+bestaudio/best"
#else
                    Format = "bestvideo[vcodec=vp8]+bestaudio/best"
#endif
                };
                if (!VideoPlayerManager.CanGetCodecs())
                {
                    // If we can't get codecs, we likely will fallback to Unity
                    optionSet.RecodeVideo = VideoRecodeFormat.Mp4;
                    optionSet.PostprocessorArgs = "ffmpeg:-preset ultrafast";
                }
                if (TryGetCookies(out string cookiesFile))
                    optionSet.Cookies = cookiesFile;
                RunResult<string> runResult;
                runResult = req.Options.AudioOnly
                    ? await ytdl.RunAudioDownload(url, overrideOptions: optionSet)
                    : await ytdl.RunVideoDownload(url, overrideOptions: optionSet);
                if (!runResult.Success)
                {
                    foreach (string s in runResult.ErrorOutput)
                        Logger.CurrentLogger.Error(s);
                    throw new Exception("Failed to get data!");
                }
                callback.Invoke(runResult.Data, false);
            }
            catch (Exception e)
            {
                callback.Invoke(String.Empty, false);
                Logger.CurrentLogger.Critical(e);
            }
        }
    }
}