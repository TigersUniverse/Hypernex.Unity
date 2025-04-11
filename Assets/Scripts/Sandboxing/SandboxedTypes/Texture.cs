using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Texture
    {
        internal Texture2D r;

        public Texture() => r = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        internal Texture(Texture2D r)
        {
            this.r = r;
        }

        public bool IsReadable => r.isReadable;

        public int Width => r.width;
        public int Height => r.height;

        public bool PointFilter
        {
            get => r.filterMode == FilterMode.Point;
            set => r.filterMode = value ? FilterMode.Point : FilterMode.Bilinear;
        }

        public void Resize(int width, int height)
        {
            r.Reinitialize(width, height, TextureFormat.RGBA32, false);
        }

        public void SetPixelsRaw(byte[] data)
        {
            r.SetPixelData(data, 0);
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            this.r.SetPixel(x, y, new Color32(r, g, b, a));
        }

        public void Apply()
        {
            r.Apply();
        }
    }
}