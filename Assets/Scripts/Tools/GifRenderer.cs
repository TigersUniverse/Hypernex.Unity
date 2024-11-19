#define mg
//#define uni
//#define none
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Decoder = MG.GIF.Decoder;
using Image = MG.GIF.Image;

namespace Hypernex.Tools
{
    public class GifRenderer : MonoBehaviour
    {
        //public static bool IsGif(byte[] data) => new Bitmap(new MemoryStream(data)).RawFormat.Equals(ImageFormat.Gif);
        //public static bool IsGif(byte[] data) => SixLabors.ImageSharp.Image.DetectFormat(data).Name == "GIF";
        public static bool IsGif(byte[] data)
        {
            if (data.Length < 3)
                return false;
            byte[] r = {
                data[0],
                data[1],
                data[2]
            };
            string s = Encoding.Default.GetString(r);
            return s.ToLower() == "gif";
        }
        
        private RawImage rawImage;
        private UnityEngine.UIElements.Image image;
        private byte[] d = Array.Empty<byte>();
        private bool loaded;
        private Dictionary<Texture2D, Sprite> sprites = new();

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
                    Texture2D texture2D = gifTexture.m_texture2d;
                    Sprite sprite = texture2D.ToSprite();
                    sprites.Add(texture2D, sprite);
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
                SetTexture(frames[currentFrame].m_texture2d);
            }
        }
        
        private void OnDestroy()
        {
            foreach (UniGif.GifTexture gifTexture in frames)
                Destroy(gifTexture.m_texture2d);
            frames.Clear();
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
            Image img = decoder.NextImage();
            while (img != null)
            {
                Texture2D texture2D = img.CreateTexture();
                Sprite sprite = texture2D.ToSprite();
                frames.Add(texture2D);
                sprites.Add(texture2D, sprite);
                frameDelays.Add(img.Delay / 1000.0f);
                img = decoder.NextImage();
            }
            SetTexture(frames[0]);
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
                SetTexture(frames[currentFrame]);
            }
        }

        private void OnDestroy()
        {
            foreach (Texture2D frame in frames)
                Destroy(frame);
            frames.Clear();
        }
#endif
      
#if none
        public void LoadGif(byte[] data){}
#else
        private void SetTexture(Texture2D texture2D)
        {
            if (rawImage != null)
                rawImage.texture = texture2D;
            if (image != null)
                image.sprite = sprites[texture2D];
        }
        
        void OnEnable()
        {
            rawImage = GetComponent<RawImage>();
            image = GetComponent<UnityEngine.UIElements.Image>();
            if (!loaded && d.Length > 0)
                LoadGif(d);
        }
#endif
    }
}