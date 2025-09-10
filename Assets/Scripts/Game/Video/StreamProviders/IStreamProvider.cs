using System;
using Hypernex.Networking.SandboxedClasses;

namespace Hypernex.Game.Video.StreamProviders
{
    public interface IStreamProvider
    {
        public bool IsHostnameSupported(VideoRequest req);
        public void DownloadVideo(VideoRequest req, Action<string, bool> callback);
    }
}