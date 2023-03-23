using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI
{
    public class UIThemeObject : MonoBehaviour
    {
        public UIThemeObjectType ThemeType;
        public ButtonType ButtonType;
    
        public void ApplyTheme(UITheme theme)
        {
            if (ThemeType is UIThemeObjectType.PrimaryVector or UIThemeObjectType.SecondaryVector)
            {
                Image img = GetComponent<Image>();
                if (img != null)
                    img.color = ThemeType == UIThemeObjectType.PrimaryVector ? theme.PrimaryColor : theme.SecondaryColor;
                RawImage rawimg = GetComponent<RawImage>();
                if(rawimg != null)
                    rawimg.color = ThemeType == UIThemeObjectType.PrimaryVector ? theme.PrimaryColor : theme.SecondaryColor;
            }
            else if (ThemeType is UIThemeObjectType.InverseVector)
            {
                Image img = GetComponent<Image>();
                if (img != null)
                    img.color = theme.PrimaryLabelColor;
                RawImage rawimg = GetComponent<RawImage>();
                if(rawimg != null)
                    rawimg.color = theme.PrimaryLabelColor;
            }
            else if (ThemeType is UIThemeObjectType.PrimaryText or UIThemeObjectType.SecondaryText)
            {
                TMP_Text textMeshPro = GetComponent<TMP_Text>();
                if (textMeshPro != null)
                {
                    textMeshPro.color = ThemeType == UIThemeObjectType.PrimaryText ? theme.PrimaryLabelColor : theme.SecondaryLabelColor;
                    textMeshPro.font = ThemeType == UIThemeObjectType.PrimaryText ? theme.PrimaryFont : theme.SecondaryFont;
                }
            }
            else if (ThemeType == UIThemeObjectType.ButtonText)
            {
                Button button = gameObject.GetComponent<Button>();
                if (button != null)
                {
                    button.colors = new ColorBlock
                    {
                        normalColor = theme.GetButtonThemeFromButtonType(ButtonType).ButtonNormalColor,
                        pressedColor = theme.GetButtonThemeFromButtonType(ButtonType).ButtonPressedColor,
                        disabledColor = theme.GetButtonThemeFromButtonType(ButtonType).ButtonDisabledColor,
                        highlightedColor = theme.GetButtonThemeFromButtonType(ButtonType).ButtonHighlightedColor,
                        selectedColor = theme.GetButtonThemeFromButtonType(ButtonType).ButtonSelectedColor,
                        colorMultiplier = 1
                    };
                    foreach (TMP_Text child in GetComponentsInChildren<TMP_Text>())
                    {
                        child.color = theme.GetButtonThemeFromButtonType(ButtonType).ButtonLabelColor;
                        child.font = theme.GetButtonThemeFromButtonType(ButtonType).ButtonFont;
                    }
                }
            }
        }
    }

    public enum UIThemeObjectType
    {
        PrimaryVector,
        SecondaryVector,
        PrimaryText,
        SecondaryText,
        ButtonText,
        InverseVector
    }
}