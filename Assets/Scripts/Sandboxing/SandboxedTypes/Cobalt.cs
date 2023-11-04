using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CobaltSharp;
using Hypernex.Tools;
using Nexbox;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Cobalt
    {
        internal static CobaltSharp.Cobalt c = new();

        public static void GetOptions(GetMedia getMedia, SandboxFunc callback)
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
                        options.Add(new CobaltOption(mediaResponse.url));
                    else
                    {
                        foreach (PickerItem pickerItem in mediaResponse.picker)
                            options.Add(new CobaltOption(pickerItem.url, pickerItem.thumb ?? String.Empty));
                    }

                    SandboxFuncTools.InvokeSandboxFunc(callback, new CobaltOptions(options));
                }));
            }).Start();
        }
    }

    public class CobaltOption
    {
        private string url;
        private string thumbnail;

        public CobaltOption() { throw new Exception("Cannot instantiate CobaltOption!"); }

        internal CobaltOption(string url, string thumbnail = "")
        {
            this.url = url;
            this.thumbnail = thumbnail;
        }

        public void Download(SandboxFunc onDone)
        {
            string pathToCobalt = Path.Combine(Application.streamingAssetsPath, "Cobalt");
            if (!Directory.Exists(pathToCobalt))
                Directory.CreateDirectory(pathToCobalt);
            new Thread(() =>
            {
                GetStream getStream = new GetStream(url);
                StreamResponse streamResponse = Cobalt.c.GetStream(getStream);
                if (streamResponse.status != Status.Success)
                {
                    QuickInvoke.InvokeActionOnMainThread(new Action(() => SandboxFuncTools.InvokeSandboxFunc(onDone, null)));
                    return;
                }
                string np = Path.Combine(pathToCobalt, streamResponse.FileName);
                FileStream fs = new FileStream(np, FileMode.Create, FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);
                using MemoryStream ms = new MemoryStream();
                streamResponse.Stream!.CopyTo(ms);
                byte[] data = ms.ToArray();
                fs.Write(data, 0, data.Length);
                fs.Dispose();
                streamResponse.Dispose();
                CobaltDownload cobaltDownload = new CobaltDownload(np);
                QuickInvoke.InvokeActionOnMainThread(new Action(() => SandboxFuncTools.InvokeSandboxFunc(onDone, cobaltDownload)));
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

        public CobaltDownload() { throw new Exception("Cannot instantiate CobaltDownload!"); }
        internal CobaltDownload(string ptf) => PathToFile = ptf;
    }

    public enum CobaltType
    {
        Video,
        Audio
    }
}