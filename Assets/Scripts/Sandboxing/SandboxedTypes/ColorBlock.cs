namespace Hypernex.Sandboxing.SandboxedTypes
{
    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color selectedColor;
        public Color disabledColor;
        public float colorMultiplier;
        public float fadeDuration;

        internal UnityEngine.UI.ColorBlock ToUnityColorBlock() => new()
        {
            normalColor = normalColor.ToUnityColor(),
            highlightedColor = highlightedColor.ToUnityColor(),
            pressedColor = pressedColor.ToUnityColor(),
            selectedColor = selectedColor.ToUnityColor(),
            disabledColor = disabledColor.ToUnityColor(),
            colorMultiplier = colorMultiplier,
            fadeDuration = fadeDuration
        };

        internal static ColorBlock FromUnityColorBlock(UnityEngine.UI.ColorBlock colorBlock) => new()
        {
            normalColor = Color.FromUnityColor(colorBlock.normalColor),
            highlightedColor = Color.FromUnityColor(colorBlock.highlightedColor),
            pressedColor = Color.FromUnityColor(colorBlock.pressedColor),
            selectedColor = Color.FromUnityColor(colorBlock.selectedColor),
            disabledColor = Color.FromUnityColor(colorBlock.disabledColor),
            colorMultiplier = colorBlock.colorMultiplier,
            fadeDuration = colorBlock.fadeDuration
        };
    }
}