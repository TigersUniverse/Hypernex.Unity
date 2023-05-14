using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(RawImage))]
    public class GifRenderer : MonoBehaviour
    {
        public List<UniGif.GifTexture> Frames => new(frames);
        public int CurrentFrame => currentFrame;
        public bool LoadedGif => loaded;

        private byte[] d = Array.Empty<byte>();
        private RawImage rawImage;
        private readonly List<UniGif.GifTexture> frames = new();
        private int currentFrame;
        private float time;
        private bool loaded;

        public static bool IsGif(byte[] data) => new Bitmap(new MemoryStream(data)).RawFormat.Equals(ImageFormat.Gif);

        public void LoadGif(byte[] data) => StartCoroutine(renderGif(data));

        private IEnumerator renderGif(byte[] data)
        {
            d = data;
            loaded = false;
            frames.Clear();
            currentFrame = 0;
            time = 0f;
            yield return UniGif.GetTextureListCoroutine(data, (textures, loopCount, width, height) =>
            {
                foreach (UniGif.GifTexture gifTexture in textures)
                {
                    frames.Add(gifTexture);
                }
                loaded = true;
                d = Array.Empty<byte>();
            });
        }

        void OnEnable()
        {
            rawImage = GetComponent<RawImage>();
            if (!loaded && d.Length > 0)
                LoadGif(d);
        }

        private void Update()
        {
            if (!loaded)
                return;
            time += Time.deltaTime;
            if (time >= frames[currentFrame].m_delaySec)
            {
                currentFrame = (currentFrame + 1) % frames.Count;
                time = 0.0f;
                rawImage.texture = frames[currentFrame].m_texture2d;
            }
        }
    }
}