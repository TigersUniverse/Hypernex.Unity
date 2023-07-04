#define mg
//#define uni
//#define none
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MG.GIF;
using UnityEngine;
using UnityEngine.UI;
using Image = MG.GIF.Image;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(RawImage))]
    public class GifRenderer : MonoBehaviour
    {
        public static bool IsGif(byte[] data) => new Bitmap(new MemoryStream(data)).RawFormat.Equals(ImageFormat.Gif);
        
        private RawImage rawImage;
        private byte[] d = Array.Empty<byte>();
        private bool loaded;

#if uni
        public int CurrentFrame => currentFrame;
        public bool LoadedGif => loaded;

        private int currentFrame;
        private float time;
        public List<UniGif.GifTexture> Frames => new(frames);
        private readonly List<UniGif.GifTexture> frames = new();

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
#endif

#if mg
        public List<Texture2D> Frames => new(frames);
        private Decoder decoder;
        private readonly List<Texture2D> frames = new();
        private List<float> frameDelays = new ();
        private int currentFrame = 0;
        private float time = 0.0f;
        
        public void LoadGif(byte[] data) => renderGif(data);
        
        private void renderGif(byte[] data)
        {
            d = data;
            loaded = false;
            if(decoder != null)
                decoder.Dispose();
            decoder = new Decoder(data);
            Image image = decoder.NextImage();
            while (image != null)
            {
                frames.Add(image.CreateTexture());
                frameDelays.Add(image.Delay / 1000.0f);
                image = decoder.NextImage();
            }
            rawImage.texture = frames[0];
            loaded = true;
        }

        private void Update()
        {
            if (!loaded)
                return;
            time += Time.deltaTime;
            if (time >= frameDelays[currentFrame])
            {
                currentFrame = (currentFrame + 1) % frames.Count;
                time = 0.0f;
                rawImage.texture = frames[currentFrame];
            }
        }
#endif
      
#if none
        public void LoadGif(byte[] data){}
#else
        void OnEnable()
        {
            rawImage = GetComponent<RawImage>();
            if (!loaded && d.Length > 0)
                LoadGif(d);
        }
#endif
    }
}