using UnityEngine;

namespace Hypernex.Tools
{
    public static class CursorTools
    {
        internal static Texture2D newMouse;
        internal static Texture2D newCircle;

        public static void ToggleMouseVisibility(bool value)
        {
            if(value == Cursor.visible) return;
            Cursor.visible = value;
        }

        public static void ToggleMouseLock(bool value)
        {
            if ((Cursor.lockState == CursorLockMode.None) != value)
                Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public static void UpdateMouseIcon(bool circle, Color color)
        {
            if(newMouse != null)
                Object.Destroy(newMouse);
            if(newCircle != null)
                Object.Destroy(newCircle);
            newMouse = CloneWithNewColor(Init.Instance.MouseTexture, color);
            newCircle = CloneWithNewColor(Init.Instance.CircleMouseTexture, color);
            UpdateMouseIcon(circle);
        }

        public static void UpdateMouseIcon(bool circle)
        {
            if(newMouse == null || newCircle == null)
                return;
            Cursor.SetCursor(circle ? newCircle : newMouse, Vector2.zero, CursorMode.Auto);
        }

        private static Texture2D CloneWithNewColor(Texture2D source, Color color)
        {
            Texture2D n = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            for (int x = 0; x < source.width; x++)
            {
                for (int y = 0; y < source.height; y++)
                {
                    Color p = source.GetPixel(x, y);
                    Color nc = p * color;
                    n.SetPixel(x, y, nc);
                }
            }
            n.Apply();
            return n;
        }
    }
}