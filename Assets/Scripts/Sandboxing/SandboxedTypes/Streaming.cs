using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using Hypernex.Configuration;
using Hypernex.Game.Video;
using Hypernex.Game.Video.StreamProviders;
using Hypernex.Networking.SandboxedClasses;
using Hypernex.Tools;
using Nexbox;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Streaming
    {
        public static void Download(VideoRequest req, object onDone)
        {
            try
            {
                if (req.GetNeedsClientFetch())
                {
                    bool found = false;
                    foreach (IStreamProvider provider in VideoPlayerManager.StreamProviders)
                    {
                        if(!provider.IsHostnameSupported(req)) continue;
                        found = true;
                        provider.DownloadVideo(req, (url, isStream) =>
                        {
                            if (isStream)
                            {
                                SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                                    new StreamDownload(url, true));
                                return;
                            }
                            if (File.Exists(url))
                            {
                                SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                                    new StreamDownload(url, false));
                                return;
                            }
                            SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone));
                        });
                        break;
                    }
                    if(!found)
                        SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone));
                    return;
                }
                string url = req.GetDownloadUrl();
                Uri uri = new Uri(url);
                bool trusted = !ConfigManager.LoadedConfig.UseTrustedURLs;
                if (!trusted)
                {
                    foreach (Uri trustedUri in ConfigManager.LoadedConfig.TrustedURLs.Union(Config.DefaultURLs)
                                 .Select(x => new Uri(x)))
                    {
                        if (uri.Host != trustedUri.Host) continue;
                        trusted = true;
                        break;
                    }
                }
                if (!trusted)
                {
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone));
                    return;
                }
                if (req.GetIsStream())
                {
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                        new StreamDownload(url, true));
                    return;
                }
                using WebClient w = new WebClient();
                w.OpenRead(uri);
                string filename = Path.GetFileName(uri.LocalPath);
                string headers = w.ResponseHeaders["content-disposition"];
                if(!string.IsNullOrEmpty(headers))
                    filename = new ContentDisposition(headers).FileName;
                string p = Path.Combine(Init.Instance.GetMediaLocation(), filename);
                DownloadTools.DownloadFile(url, p, s =>
                {
                    if (!File.Exists(s))
                    {
                        SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone));
                        return;
                    }
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                        new StreamDownload(s, false));
                }, forceNewHttp: true);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
                SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone));
            }
        }

        public class StreamDownload
        {
            internal string pathToFile;
            internal bool isStream;

            public StreamDownload() { throw new Exception("Cannot Instantiate StreamDownload"); }

            internal StreamDownload(string pathToFile, bool isStream)
            {
                this.pathToFile = pathToFile;
                this.isStream = isStream;
            }
        }
    }
}