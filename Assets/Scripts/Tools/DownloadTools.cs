using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

public static class DownloadTools
{
    public static int MaxThreads { get; set; } = 50;
    private static readonly List<DownloadMeta> Queue = new();
    private static readonly Dictionary<DownloadMeta, Thread> RunningThreads = new();

    public static void DownloadBytes(string url, Action<byte[]> OnDownload, Action<DownloadProgressChangedEventArgs> DownloadProgress = null)
    {
        DownloadMeta meta = new DownloadMeta
        {
            url = url,
            done = OnDownload
        };
        if (DownloadProgress != null)
            meta.progress = DownloadProgress;
        Queue.Add(meta);
        Logger.CurrentLogger.Log("Added " + url + " to download queue!");
        Check();
    }

    private static void Check()
    {
        if (Queue.Count <= 0 || RunningThreads.Count >= MaxThreads)
            return;
        foreach (DownloadMeta downloadMeta in new List<DownloadMeta>(Queue))
        {
            if(RunningThreads.Count >= MaxThreads)
                return;
            Logger.CurrentLogger.Log("Beginning Download for " + downloadMeta.url);
            Queue.Remove(downloadMeta);
            Thread t = new Thread(() =>
            {
                using WebClient wc = new WebClient();
                if(downloadMeta.progress != null)
                    wc.DownloadProgressChanged += (sender, args) =>
                        QuickInvoke.InvokeActionOnMainThread(downloadMeta.progress, args);
                byte[] d = wc.DownloadData(new Uri(downloadMeta.url));
                QuickInvoke.InvokeActionOnMainThread(downloadMeta.done, d);
                Check();
                RunningThreads.Remove(downloadMeta);
            });
            RunningThreads.Add(downloadMeta, t);
            t.Start();
        }
    }
}

public class DownloadMeta
{
    public string url;
    public Action<DownloadProgressChangedEventArgs> progress;
    public Action<byte[]> done;
}