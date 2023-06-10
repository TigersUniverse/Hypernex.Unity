namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Color
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public Color()
        {
            R = 0f;
            G = 0f;
            B = 0f;
            A = 1f;
        }
        
        public Color(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
            A = 1f;
        }
        
        public Color(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        internal UnityEngine.Color ToUnityColor() => new (R, G, B, A);
        internal static Color FromUnityColor(UnityEngine.Color color) => new (color.r, color.g, color.b, color.a);
    }
}