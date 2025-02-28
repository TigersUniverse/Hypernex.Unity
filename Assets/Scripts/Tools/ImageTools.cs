using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.Tools
{
    public static class ImageTools
    {
        private static Dictionary<string, Texture2D> CachedImages = new();

        public static Texture2D BytesToTexture2D(string id, byte[] bytes, bool ignoreCache = false)
        {
            if (!ignoreCache && CachedImages.ContainsKey(id))
                return CachedImages[id];
            Texture2D t = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            t.LoadImage(bytes);
            CachedImages.Add(id, t);
            return t;
        }
    }
}