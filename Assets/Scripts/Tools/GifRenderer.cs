using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using MG.GIF;
using UnityEngine;
using UnityEngine.UI;
using Image = MG.GIF.Image;

[RequireComponent(typeof(RawImage))]
public class GifRenderer : MonoBehaviour
{
    public List<Texture2D> Frames => new(frames);
    public int CurrentFrame => currentFrame;
    
    private RawImage rawImage;
    private readonly List<Texture2D> frames = new();
    private readonly List<float> frameDelay = new();
    private int currentFrame;
    private float time;

    public static bool IsGif(byte[] data) => new Bitmap(new MemoryStream(data)).RawFormat.Equals(ImageFormat.Gif);
    
    public void LoadGif(byte[] data)
    {
        frames.Clear();
        frameDelay.Clear();
        using (Decoder decoder = new Decoder(data))
        {
            Image img = decoder.NextImage();
            while (img != null)
            {
                frames.Add(img.CreateTexture());
                frameDelay.Add(img.Delay / 1000.0f);
                img = decoder.NextImage();
            }
            rawImage.texture = frames.First() ?? Texture2D.whiteTexture;
        }
    }

    void OnEnable() => rawImage = GetComponent<RawImage>();

    private void Update()
    {
        if (frames.Count <= 0)
            return;
        time += Time.deltaTime;
        if (time >= frameDelay[currentFrame])
        {
            currentFrame = (currentFrame + 1) % frames.Count;
            time = 0.0f;
            rawImage.texture = frames[currentFrame];
        }
    }
}