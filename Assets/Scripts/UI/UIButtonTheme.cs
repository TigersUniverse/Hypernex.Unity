using TMPro;
using UnityEngine;

public class UIButtonTheme : MonoBehaviour
{
    public ButtonType ButtonType;
    
    public Color ButtonNormalColor;
    public Color ButtonHighlightedColor;
    public Color ButtonPressedColor;
    public Color ButtonSelectedColor;
    public Color ButtonDisabledColor;
    public Color ButtonLabelColor;
    public TMP_FontAsset ButtonFont;
}

public enum ButtonType
{
    Primary,
    Secondary,
    Success,
    Warning,
    Error
}