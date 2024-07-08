using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Hypernex.Configuration;
using HypernexSharp.APIObjects;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools
{
    public static class DownloadTools
    {
        internal static string DownloadsPath;
        internal static bool forceHttpClient;
        private static readonly Dictionary<string, byte[]> Cache = new();
        private static readonly Queue<DownloadMeta> Queue = new();
        private static readonly Dictionary<DownloadMeta, Thread> RunningThreads = new();
        private static HttpClient httpClient = new ();

        public static void DownloadBytes(string url, Action<byte[]> OnDownload, Action<DownloadProgressChangedEventArgs> DownloadProgress = null, bool skipCache = false)
        {
#if UNITY_ANDROID
            skipCache = true;
#endif
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
            Queue.Enqueue(meta);
            Logger.CurrentLogger.Debug("Added " + url + " to download queue!");
            Check();
        }

#nullable enable
        public static string? GetFileNameFromUrl(string url)
        {
            try
            {
                WebRequest webRequest = WebRequest.Create(url);
                webRequest.Method = "HEAD";
                using WebResponse webResponse = webRequest.GetResponse();
                string contentDispositionHeader = webResponse.Headers["Content-Disposition"];
                if(string.IsNullOrEmpty(contentDispositionHeader)) return null;
                const string FILE_NAME_MARKER = "filename=\"";
                int fileNameBeginIndex = contentDispositionHeader.ToLower().IndexOf(FILE_NAME_MARKER);
                return contentDispositionHeader.Substring(fileNameBeginIndex + FILE_NAME_MARKER.Length).Replace("\"", "");
            }
            catch (Exception)
            {
                return null;
            }
        }
#nullable restore

        public static string GetStringHash(string s)
        {
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string GetFileHash(string file)
        {
            using MD5 md5 = MD5.Create();
            using FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite,
                FileShare.ReadWrite | FileShare.Delete);
            byte[] hash = md5.ComputeHash(fileStream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static void DownloadFile(string url, string output, Action<string> OnDownload,
            string knownFileHash = null, Action<DownloadProgressChangedEventArgs> DownloadProgress = null, bool ignoreDownloadsPath = false)
        {
            if (!Directory.Exists(DownloadsPath))
                Directory.CreateDirectory(DownloadsPath);
            string fileOutput = Path.Combine(DownloadsPath, output);
            if (ignoreDownloadsPath)
                fileOutput = output;
            bool fileExists = File.Exists(fileOutput);
            bool isHashSame = false;
            if (fileExists && !string.IsNullOrEmpty(knownFileHash))
                isHashSame = GetFileHash(fileOutput) == knownFileHash;
            if(fileExists && isHashSame)
                QuickInvoke.InvokeActionOnMainThread(OnDownload, fileOutput);
            else
            {
                DownloadMeta meta = new DownloadMeta
                {
                    url = url,
                    done = b =>
                    {
                        try
                        {
                            //File.WriteAllBytes(fileOutput, b);
                            FileStream fs = new FileStream(fileOutput, FileMode.Create, FileAccess.ReadWrite,
                                FileShare.ReadWrite | FileShare.Delete);
                            fs.Write(new ReadOnlySpan<byte>(b));
                            fs.Dispose();
                            //Array.Clear(b, 0, b.Length);
                            QuickInvoke.InvokeActionOnMainThread(OnDownload, fileOutput);
                        } catch(Exception e){Logger.CurrentLogger.Critical(e);}
                    },
                    progress = p =>
                    {
                        if (DownloadProgress != null)
                            QuickInvoke.InvokeActionOnMainThread(DownloadProgress, p);
                    },
                    skipCache = true
                };
                Queue.Enqueue(meta);
                Logger.CurrentLogger.Debug("Added " + url + " to download queue!");
                Check();
            }
        }

        public static void ClearCache() => Cache.Clear();

        private static void Check()
        {
            if (Queue.Count <= 0 || RunningThreads.Count >= ConfigManager.LoadedConfig.DownloadThreads)
                return;
            if(RunningThreads.Count >= ConfigManager.LoadedConfig.DownloadThreads)
                return;
            DownloadMeta downloadMeta = Queue.Dequeue();
            Logger.CurrentLogger.Debug("Beginning Download for " + downloadMeta.url);
            Thread t = new Thread(async () =>
            {
                try
                {
                    if(AssetBundleTools.Platform != BuildPlatform.Windows || forceHttpClient)
                    {
                        byte[] d = await httpClient.GetByteArrayAsync(new Uri(downloadMeta.url));
                        Logger.CurrentLogger.Debug("Finished download for " + downloadMeta.url);
                        if (!downloadMeta.skipCache)
                            AttemptAddToCache(downloadMeta.url, d);
                        QuickInvoke.InvokeActionOnMainThread(downloadMeta.done, d);
                        RunningThreads.Remove(downloadMeta);
                    }
                    else
                    {
                        using WebClient wc = new WebClient();
                        if (downloadMeta.progress != null)
                            wc.DownloadProgressChanged += (sender, args) =>
                                QuickInvoke.InvokeActionOnMainThread(downloadMeta.progress, args);
                        wc.DownloadDataCompleted += (sender, args) =>
                        {
                            Logger.CurrentLogger.Debug("Finished download for " + downloadMeta.url);
                            if (!downloadMeta.skipCache)
                                AttemptAddToCache(downloadMeta.url, args.Result);
                            QuickInvoke.InvokeActionOnMainThread(downloadMeta.done, args.Result);
                            RunningThreads.Remove(downloadMeta);
                        };
                        await wc.DownloadDataTaskAsync(new Uri(downloadMeta.url));
                    }
                } catch(Exception e){Logger.CurrentLogger.Critical(e);}
            });
            RunningThreads.Add(downloadMeta, t);
            t.Start();
        }

        private static void AttemptAddToCache(string url, byte[] data)
        {
            // Count size (MB)
            double dataSize = data.Length / (1024.0 * 1024.0);
            if(dataSize > ConfigManager.LoadedConfig.MaxMemoryStorageCache)
                return;
            double s = 0.0;
            foreach (byte[] d in Cache.Values)
                s += d?.Length / (1024.0 * 1024.0) ?? 0;
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
            if(!Cache.ContainsKey(url))
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