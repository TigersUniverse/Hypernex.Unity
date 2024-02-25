﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CobaltSharp;
using Hypernex.Configuration;
using Hypernex.Tools;
using Nexbox;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

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
                try
                {
                    Uri u = new Uri(getMedia.url);
                    string h = u.Host.ToLower();
                    bool isYoutube = h is "youtube.com" or "youtu.be" or "m.youtube.com";
                    bool isLinux = Application.platform == RuntimePlatform.LinuxPlayer ||
                                   Application.platform == RuntimePlatform.LinuxEditor;
                    if (isYoutube && isLinux)
                        getMedia.vCodec = VideoCodec.vp9;
                }
                catch(Exception){}
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

                    if (options.Count > 0)
                        SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(callback),
                            new CobaltOptions(options));
                    else
                        TryDownloadFile(getMedia.url, SandboxFuncTools.TryConvert(callback));
                }));
            }).Start();
        }
        
        private static void TryDownloadFile(string url, object callback)
        {
            try
            {
                Uri u = new Uri(url);
                UriBuilder b = new UriBuilder(u);
                b.Query = "";
                u = new Uri(b.ToString());
                bool allowed = false;
                foreach (string trustedUrl in ConfigManager.LoadedConfig.TrustedURLs)
                {
                    try
                    {
                        Uri trustedUri = new Uri(trustedUrl);
                        if (trustedUri.Host.ToLower() == u.Host.ToLower())
                            allowed = true;
                    }
                    catch(Exception){}
                }
                if (allowed)
                {
                    string name = u.Segments.Last().TrimEnd('/');
                    string dir = Path.Combine(Application.streamingAssetsPath, "Cobalt");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    string file = Path.Combine(dir, name);
                    DownloadTools.DownloadFile(url, file, f =>
                    {
                        if (File.Exists(f))
                            QuickInvoke.InvokeActionOnMainThread(new Action(() => SandboxFuncTools.InvokeSandboxFunc(
                                SandboxFuncTools.TryConvert(callback), new CobaltOptions(new List<CobaltOption>
                                {
                                    new CobaltOption(file, true)
                                }))));
                    }, ignoreDownloadsPath: true);
                }
            }
            catch(Exception){}
        }
    }

    public class CobaltOption
    {
        private string url;
        private string thumbnail;
        private bool isLocalFile;

        public CobaltOption() { throw new Exception("Cannot instantiate CobaltOption!"); }

        internal CobaltOption(string url, string thumbnail = "")
        {
            this.url = url;
            this.thumbnail = thumbnail;
        }

        internal CobaltOption(string u, bool b)
        {
            url = u;
            isLocalFile = b;
        }

        private void ConvertToVP8(string file, Action onExit)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i {file} -c:v libvpx -crf 10 -b:v 1M -c:a libvorbis {file}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            process.Exited += (sender, args) => onExit.Invoke();
            process.Start();
        }

        public void Download(object onDone)
        {
            if(!isLocalFile)
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
                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                            SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), null)));
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
                    if (Application.platform is RuntimePlatform.LinuxEditor or RuntimePlatform.LinuxPlayer)
                    {
                        try
                        {
                            ConvertToVP8(np,
                                () => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                                        cobaltDownload))));
                        }
                        catch (Exception e)
                        {
                            Logger.CurrentLogger.Critical(e);
                            QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                                    cobaltDownload)));
                        }

                        return;
                    }

                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), cobaltDownload)));
                }).Start();
            }
            else
            {
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), new CobaltDownload(url))));
            }
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