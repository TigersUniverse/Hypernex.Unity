using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CobaltSharp;
using Hypernex.Configuration;
using Hypernex.Game.Video;
using Hypernex.Tools;
using Nexbox;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Cobalt
    {
        internal static CobaltSharp.Cobalt c = new();

        public static void GetOptions(GetMedia getMedia, object callback)
        {
            new Thread(() =>
            {
                getMedia.aFormat = AudioFormat.mp3;
                getMedia.vCodec = VideoCodec.h264;
                List<CobaltOption> options = new List<CobaltOption>();
                MediaResponse mediaResponse = c.GetMedia(getMedia);
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    if (mediaResponse.status == Status.Stream)
                        options.Add(new CobaltOption(mediaResponse));
                    else if(mediaResponse.status == Status.Picker)
                    {
                        foreach (PickerItem pickerItem in mediaResponse.picker)
                            options.Add(new CobaltOption(pickerItem, pickerItem.thumb ?? String.Empty));
                    }
                    else if(mediaResponse.status == Status.Error)
                        try
                        {
                            options.Add(new CobaltOption(new Uri(getMedia.url), getMedia.url));
                        }catch(Exception){}
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(callback), new CobaltOptions(options));
                }));
            }).Start();
        }
    }

    public class CobaltOption
    {
        private MediaResponse? mediaResponse;
        private PickerItem? pickerItem;
        private string thumbnail;
        private Uri uri;
        private string url;

        public CobaltOption() { throw new Exception("Cannot instantiate CobaltOption!"); }

        internal CobaltOption(MediaResponse mediaResponse, string thumbnail = "")
        {
            this.mediaResponse = mediaResponse;
            this.thumbnail = thumbnail;
        }
        
        internal CobaltOption(PickerItem pickerItem, string thumbnail = "")
        {
            this.pickerItem = pickerItem;
            this.thumbnail = thumbnail;
        }

        internal CobaltOption(Uri uri, string url)
        {
            this.uri = uri;
            this.url = url;
        }

        public void Download(object onDone)
        {
            string pathToCobalt = Path.Combine(Application.streamingAssetsPath, "Cobalt");
            if (!Directory.Exists(pathToCobalt))
                Directory.CreateDirectory(pathToCobalt);
            if (uri != null)
            {
                bool trusted = !ConfigManager.LoadedConfig.UseTrustedURLs;
                if(!trusted)
                {
                    foreach (Uri trustedUri in ConfigManager.LoadedConfig.TrustedURLs.Select(x => new Uri(x)))
                    {
                        if (uri.Host != trustedUri.Host) continue;
                        trusted = true;
                        break;
                    }
                }
                if (!trusted)
                {
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), null)));
                    return;
                }
                if (VideoPlayerManager.IsStream(uri))
                {
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                            new CobaltDownload(url, true))));
                }
                else
                {
                    // TODO: Check URI ending to see if the extension is a valid one
                    string fileName = DownloadTools.GetFileNameFromUrl(url) ?? DownloadTools.GetStringHash(url);
                    string filePath = Path.Combine(pathToCobalt, fileName);
                    DownloadTools.DownloadFile(url, filePath,
                        downloadedFile => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                            SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                                new CobaltDownload(downloadedFile, false)))));
                }
                return;
            }
            new Thread(() =>
            {
                StreamResponse streamResponse;
                if (mediaResponse != null)
                    streamResponse = Cobalt.c.GetStream(mediaResponse.Value);
                else if(pickerItem != null)
                    streamResponse = Cobalt.c.GetStream(pickerItem.Value);
                else
                {
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), null)));
                    return;
                }
                if (streamResponse.status != Status.Success)
                {
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), null)));
                    return;
                }

                string np = Path.Combine(pathToCobalt, streamResponse.FileName);
                FileStream fs = new FileStream(np, FileMode.Create, FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete);
                using MemoryStream ms = new MemoryStream();
                streamResponse.Stream!.CopyTo(ms);
                byte[] data = ms.ToArray();
                fs.Write(data, 0, data.Length);
                fs.Dispose();
                streamResponse.Dispose();
                CobaltDownload cobaltDownload = new CobaltDownload(np, false);
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), cobaltDownload)));
            }).Start();
        }
    }

    public class CobaltOptions
    {
        public CobaltOption[] Options;
        
        public CobaltOptions() { throw new Exception("Cannot instantiate CobaltOption!"); }
        internal CobaltOptions(List<CobaltOption> cos) => Options = cos.ToArray();
    }

    public class CobaltDownload
    {
        internal string PathToFile;
        internal bool isStream;

        public CobaltDownload() { throw new Exception("Cannot instantiate CobaltDownload!"); }

        internal CobaltDownload(string ptf, bool s)
        {
            PathToFile = ptf;
            isStream = s;
        }
    }

    public enum CobaltType
    {
        Video,
        Audio
    }
}