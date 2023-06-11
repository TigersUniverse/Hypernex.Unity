using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Hypernex.Configuration;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools
{
    public static class DownloadTools
    {
        internal static string DownloadsPath;
        private static readonly Dictionary<string, byte[]> Cache = new();
        private static readonly List<DownloadMeta> Queue = new();
        private static readonly Dictionary<DownloadMeta, Thread> RunningThreads = new();

        public static void DownloadBytes(string url, Action<byte[]> OnDownload, Action<DownloadProgressChangedEventArgs> DownloadProgress = null, bool skipCache = false)
        {
            try
            {
                if (Cache.ContainsKey(url) && !skipCache)
                {
                    QuickInvoke.InvokeActionOnMainThread(OnDownload, Cache[url]);
                    return;
                }
            }
            catch(Exception){ClearCache();}
            DownloadMeta meta = new DownloadMeta
            {
                url = url,
                done = OnDownload,
                skipCache = skipCache
            };
            if (DownloadProgress != null)
                meta.progress = DownloadProgress;
            Queue.Add(meta);
            Logger.CurrentLogger.Log("Added " + url + " to download queue!");
            Check();
        }

        private static List<string> accessedFileOutputs = new ();

        public static void DownloadFile(string url, string output, Action<string> OnDownload,
            Action<DownloadProgressChangedEventArgs> DownloadProgress = null)
        {
            if (!Directory.Exists(DownloadsPath))
                Directory.CreateDirectory(DownloadsPath);
            string fileOutput = Path.Combine(DownloadsPath, output);
            if(File.Exists(fileOutput)/* && accessedFileOutputs.Contains(fileOutput)*/)
                OnDownload.Invoke(fileOutput);
            else
            {
                DownloadMeta meta = new DownloadMeta
                {
                    url = url,
                    done = b =>
                    {
                        File.WriteAllBytes(fileOutput, b);
                        Array.Clear(b, 0, b.Length);
                        accessedFileOutputs.Add(fileOutput);
                        QuickInvoke.InvokeActionOnMainThread(OnDownload, fileOutput);
                    },
                    progress = p =>
                    {
                        if (DownloadProgress != null)
                            QuickInvoke.InvokeActionOnMainThread(DownloadProgress, p);
                    },
                    skipCache = true
                };
                Queue.Add(meta);
                Logger.CurrentLogger.Log("Added " + url + " to download queue!");
                Check();
            }
            
            
        }

        public static void ClearCache() => Cache.Clear();

        private static void Check()
        {
            if (Queue.Count <= 0 || RunningThreads.Count >= ConfigManager.LoadedConfig.DownloadThreads)
                return;
            foreach (DownloadMeta downloadMeta in new List<DownloadMeta>(Queue))
            {
                if(RunningThreads.Count >= ConfigManager.LoadedConfig.DownloadThreads)
                    return;
                Logger.CurrentLogger.Log("Beginning Download for " + downloadMeta.url);
                Queue.Remove(downloadMeta);
                Thread t = new Thread(() =>
                {
                    using WebClient wc = new WebClient();
                    if(downloadMeta.progress != null)
                        wc.DownloadProgressChanged += (sender, args) =>
                            QuickInvoke.InvokeActionOnMainThread(downloadMeta.progress, args);
                    wc.DownloadDataCompleted += (sender, args) =>
                    {
                        Logger.CurrentLogger.Log("Finished download for " + downloadMeta.url);
                        if(!downloadMeta.skipCache)
                            AttemptAddToCache(downloadMeta.url, args.Result);
                        QuickInvoke.InvokeActionOnMainThread(downloadMeta.done, args.Result);
                        Check();
                        RunningThreads.Remove(downloadMeta);
                    };
                    wc.DownloadDataAsync(new Uri(downloadMeta.url));
                
                });
                RunningThreads.Add(downloadMeta, t);
                t.Start();
            }
        }

        private static void AttemptAddToCache(string url, byte[] data)
        {
            // Count size (MB)
            double dataSize = data.Length / (1024.0 * 1024.0);
            if(dataSize > ConfigManager.LoadedConfig.MaxMemoryStorageCache)
                return;
            double s = 0.0;
            foreach (byte[] d in Cache.Values)
                s += d.Length / (1024.0 * 1024.0);
            if (s >= ConfigManager.LoadedConfig.MaxMemoryStorageCache || s + dataSize >= ConfigManager.LoadedConfig.MaxMemoryStorageCache)
            {
                while (s >= ConfigManager.LoadedConfig.MaxMemoryStorageCache || s + dataSize >= ConfigManager.LoadedConfig.MaxMemoryStorageCache)
                {
                    if(Cache.Count <= 0)
                        break;
                    Cache.Remove(Cache.Last().Key);
                    foreach (byte[] d in Cache.Values)
                        s += d.Length / (1024.0 * 1024.0);
                }
            }
            Cache.Add(url, data);
        }
    }

    public class DownloadMeta
    {
        public string url;
        public Action<DownloadProgressChangedEventArgs> progress;
        public Action<byte[]> done;
        public bool skipCache;
    }
}