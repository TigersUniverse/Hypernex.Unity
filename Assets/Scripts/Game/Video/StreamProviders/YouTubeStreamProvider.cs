using System;
using System.IO;
using System.Threading.Tasks;
using Hypernex.Networking.SandboxedClasses;
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
                    , ExtractorArgs = "youtube:player_client=default,web_safari;player_js_version=actual"
                };
                if (!VideoPlayerManager.CanGetCodecs())
                {
                    // If we can't get codecs, we likely will fallback to Unity
                    optionSet.RecodeVideo = VideoRecodeFormat.Mp4;
                    optionSet.PostprocessorArgs = "ffmpeg:-preset ultrafast";
                }
                if (TryGetCookies(out string cookiesFile))
                    optionSet.Cookies = cookiesFile;
#if VLC
                if (!req.Options.AudioOnly && Init.Instance.StreamYoutube)
                {
                    (string, bool) ls = await PlayVideoFromVideoStream(url, optionSet.Cookies);
                    if (ls.Item2)
                    {
                        callback.Invoke(ls.Item1, true);
                        return;
                    }
                }
#endif
                RunResult<string> runResult;
                runResult = req.Options.AudioOnly
                    ? await ytdl.RunAudioDownload(url, overrideOptions: optionSet)
                    : await ytdl.RunVideoDownload(url, overrideOptions: optionSet);
                if (!runResult.Success)
                {
                    foreach (string s in runResult.ErrorOutput)
                        Logger.CurrentLogger.Error(s);
                    if(string.IsNullOrEmpty(runResult.Data) || !File.Exists(runResult.Data))
                        throw new Exception("Failed to get data!");
                    throw new Exception("Failed to get data!");
                }
                string newFileLocation =
                    Path.Combine(Init.Instance.GetMediaLocation(), Path.GetFileName(runResult.Data));
                if(File.Exists(newFileLocation))
                    File.Delete(newFileLocation);
                File.Move(runResult.Data!, newFileLocation);
                callback.Invoke(newFileLocation, false);
            }
            catch (Exception e)
            {
                callback.Invoke(String.Empty, false);
                Logger.CurrentLogger.Critical(e);
            }
        }

        private async Task<(string, bool)> PlayVideoFromVideoStream(string url, string cookies = "")
        {
            OptionSet optionSet = new OptionSet
            {
                Format = "best",
                GetUrl = true
            };
            if (!string.IsNullOrEmpty(cookies))
                optionSet.Cookies = cookies;
            string lastOutput = String.Empty;
            RunResult<string> res =
                await ytdl.RunWithOptions(url, optionSet, output: new Progress<string>(s => lastOutput = s));
            if (!res.Success || string.IsNullOrEmpty(lastOutput)) return (String.Empty, false);
            Logger.CurrentLogger.Log(lastOutput);
            try
            {
                Uri _ = new Uri(lastOutput);
                return (lastOutput, true);
            }
            catch (Exception)
            {
                return (String.Empty, false);
            }
        }
    }
}