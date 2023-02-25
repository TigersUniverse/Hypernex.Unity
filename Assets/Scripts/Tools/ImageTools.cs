using System.Drawing;
using System.IO;
using UnityEngine;

public static class ImageTools
{
    public static Texture2D BytesToTexture2D(byte[] bytes)
    {
        Bitmap b = new Bitmap(new MemoryStream(bytes));
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
        t.Apply();
        return t;
    }
}