using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hypernex.UI
{
    public class UIButtonTheme : MonoBehaviour
    {
        public ButtonType ButtonType;
    
        [FormerlySerializedAs("ButtonNormalColor")] public Color ButtonColor;
        public Color ButtonLabelColor;
        public TMP_FontAsset ButtonFont;
    }

    public enum ButtonType
    {
        Blue,
        Grey,
        Green,
        Yellow,
        Red,
        Orange
    }
}