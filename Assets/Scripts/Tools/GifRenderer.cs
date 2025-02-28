//#define gifloader
#define mg
//#define uni
//#define none
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
#if gifloader
using System.Threading;
using System.Threading.Tasks;
using B83.Image.GIF;
#endif
#if mg
using System.Threading;
using Decoder = MG.GIF.Decoder;
using Image = MG.GIF.Image;
#endif

namespace Hypernex.Tools
{
    [RequireComponent(typeof(RawImage))]
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
        private List<Image> rawImages = new List<Image>();
        
        public void LoadGif(byte[] data) => StartCoroutine(renderGif(data));
        
        private IEnumerator renderGif(byte[] data)
        {
            d = data;
            loaded = false;
            Thread t = new Thread(() =>
            {
                if (decoder != null)
                    decoder.Dispose();
                decoder = new Decoder(data);
                Image image = decoder.NextImage();
                while (image != null)
                {
                    rawImages.Add((Image) image.Clone());
                    image = decoder.NextImage();
                }
            });
            t.Start();
            yield return new WaitUntil(() => !t.IsAlive);
            foreach (Image image in rawImages)
            {
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    frames.Add(image.CreateTexture());
                    frameDelays.Add(image.Delay / 1000.0f);
                    if (rawImages.Count == frames.Count)
                    {
                        rawImage.texture = frames[0];
                        loaded = true;
                    }
                }));
            }
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

        private void OnDestroy()
        {
            foreach (Texture2D frame in frames)
                Destroy(frame);
            frames.Clear();
        }
#endif

#if gifloader
        private MemoryStream memoryStream;
        private GIFImage gifImage;
        private Coroutine coroutine;

        public void LoadGif(byte[] data) => StartCoroutine(LoadGifCoroutine(data));
        
        private void StartAnimator() => coroutine = StartCoroutine(gifImage.RunAnimation(OnUpdateTexture));

        private IEnumerator LoadGifCoroutine(byte[] data)
        {
            Thread t = new Thread(() =>
            {
                memoryStream = new MemoryStream(data);
                GIFLoader gifLoader = new GIFLoader();
                gifImage = gifLoader.Load(memoryStream);
            });
            t.Start();
            yield return new WaitUntil(() => !t.IsAlive);
            loaded = true;
            StartAnimator();
        }

        private void OnUpdateTexture(Texture2D texture2D) => rawImage.texture = texture2D;

        private void OnDestroy()
        {
            StopCoroutine(coroutine);
            memoryStream.Dispose();
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
#if gifloader
            else if (loaded)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                    coroutine = null;
                }
                StartAnimator();
            }
#endif
        }
#endif
    }
}