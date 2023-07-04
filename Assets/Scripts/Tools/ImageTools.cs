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
            /*MemoryStream ms = new MemoryStream(bytes);
            Bitmap b = new Bitmap(ms);
            Texture2D t = new Texture2D(b.Width, b.Height);
            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    System.Drawing.Color pixelColor = b.GetPixel(x, y);
                    UnityEngine.Color unity_pixelColor =
                        new UnityEngine.Color(pixelColor.R / 255.0f, pixelColor.G / 255.0f, 
                            pixelColor.B / 255.0f, pixelColor.A / 255.0f);
                    t.SetPixel(x, b.Height - y, unity_pixelColor);
                }
            }
            b.Dispose();
            ms.Dispose();*/
            Texture2D t = new Texture2D(1,1);
            t.LoadImage(bytes);
            t.Apply();
            CachedImages.Add(id, t);
            return t;
        }
    }
}