using System;
using Hypernex.Game;
using Hypernex.UI.Templates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace Hypernex.UI
{
    public class UIThemeObject : MonoBehaviour
    {
        public UIThemeObjectType ThemeType;
        public ButtonType ButtonType;
    
        public void ApplyTheme(UITheme theme)
        {
            if (ThemeType is UIThemeObjectType.BackgroundColor1 or UIThemeObjectType.BackgroundColor2 or UIThemeObjectType.BackgroundColor3 or UIThemeObjectType.BackgroundColor4 or UIThemeObjectType.BackgroundColor5)
            {
                Color color = theme.BackgroundColor1;
                if (ThemeType == UIThemeObjectType.BackgroundColor2)
                    color = theme.BackgroundColor2;
                if (ThemeType == UIThemeObjectType.BackgroundColor3)
                    color = theme.BackgroundColor3;
                if (ThemeType == UIThemeObjectType.BackgroundColor4)
                    color = theme.BackgroundColor4;
                if (ThemeType == UIThemeObjectType.BackgroundColor5)
                    color = theme.BackgroundColor5;
                Image img = GetComponent<Image>();
                if (img != null)
                    img.color = color;
                RawImage rawimg = GetComponent<RawImage>();
                if (rawimg != null)
                    rawimg.color = color;
                TMP_InputField inputField = gameObject.GetComponent<TMP_InputField>();
                if (inputField != null)
                {
                    inputField.onSelect.RemoveAllListeners();
                    inputField.onSelect.AddListener(_ =>
                    {
                        if (!LocalPlayer.IsVR)
                            return;
                        KeyboardTemplate.GetKeyboardTemplateByLanguage("en").RequestInput(s => inputField.text = s);
                    });
                }
            }
            else if (ThemeType is UIThemeObjectType.PrimaryColorVector)
            {
                Image img = GetComponent<Image>();
                if (img != null)
                    img.color = theme.PrimaryColorTheme;
                RawImage rawimg = GetComponent<RawImage>();
                if(rawimg != null)
                    rawimg.color = theme.PrimaryColorTheme;
            }
            else if (ThemeType is UIThemeObjectType.FirstText or UIThemeObjectType.SecondText or UIThemeObjectType.ThirdText or UIThemeObjectType.LinkText)
            {
                TMP_Text textMeshPro = GetComponent<TMP_Text>();
                if (textMeshPro != null)
                {
                    Color color = theme.FirstLabelColor;
                    TMP_FontAsset font = theme.FirstLabelFont;
                    if (ThemeType == UIThemeObjectType.SecondText)
                    {
                        color = theme.SecondLabelColor;
                        font = theme.SecondaryLabelFont;
                    }
                    if (ThemeType == UIThemeObjectType.ThirdText)
                    {
                        color = theme.ThirdLabelColor;
                        font = theme.ThirdLabelFont;
                    }
                    if (ThemeType == UIThemeObjectType.LinkText)
                    {
                        color = theme.LinkLabelColor;
                        font = theme.LinkLabelFont;
                    }
                    textMeshPro.color = color;
                    textMeshPro.font = font;
                }
            }
            else if (ThemeType == UIThemeObjectType.ButtonText)
            {
                Image img = gameObject.GetComponent<Image>();
                if (img != null)
                {
                    img.color = theme.GetButtonThemeFromButtonType(ButtonType).ButtonColor;
                    foreach (TMP_Text child in GetComponentsInChildren<TMP_Text>())
                    {
                        child.color = theme.GetButtonThemeFromButtonType(ButtonType).ButtonLabelColor;
                        child.font = theme.GetButtonThemeFromButtonType(ButtonType).ButtonFont;
                    }
                    foreach (Image image in GetComponentsInChildren<Image>())
                    {
                        if(img == image) continue;
                        image.color = theme.GetButtonThemeFromButtonType(ButtonType).ButtonLabelColor;
                    }
                }
            }
            else if (ThemeType == UIThemeObjectType.BackgroundImage)
            {
                Image img = GetComponent<Image>();
                if (img != null)
                {
                    if (theme.BackgroundImage == null)
                    {
                        img.color = theme.BackgroundColor1;
                        img.sprite = null;
                    }
                    else
                    {
                        img.color = new Color(1, 1, 1, theme.BackgroundColor1.a);
                        img.sprite = theme.BackgroundImage;
                    }
                }
                RawImage rawimg = GetComponent<RawImage>();
                if(rawimg != null)
                {
                    if (theme.BackgroundImage == null)
                    {
                        rawimg.color = theme.BackgroundColor1;
                        rawimg.texture = null;
                    }
                    else
                    {
                        rawimg.color = new Color(1, 1, 1, theme.BackgroundColor1.a);
                        rawimg.texture = theme.BackgroundImage.texture;
                    }
                }
            }
            else if (ThemeType == UIThemeObjectType.LineRenderer)
            {
                LineRenderer lineRenderer = GetComponent<LineRenderer>();
                if(lineRenderer != null)
                {
                    lineRenderer.startColor = theme.PrimaryColorTheme;
                    lineRenderer.endColor = theme.PrimaryColorTheme;
                }
                XRInteractorLineVisual lineVisual = GetComponent<XRInteractorLineVisual>();
                if (lineVisual != null)
                {
                    GradientColorKey[] gradientColorKeys =
                    {
                        new GradientColorKey(theme.PrimaryColorTheme, 0),
                        new GradientColorKey(theme.BackgroundColor1, 0)
                    };
                    GradientAlphaKey[] gradientAlphaKeys =
                    {
                        new GradientAlphaKey(1, 0),
                        new GradientAlphaKey(1, 0)
                    };
                    lineVisual.validColorGradient.SetKeys(gradientColorKeys, gradientAlphaKeys);
                }
            }
        }

        private void OnEnable()
        {
            try
            {
                ApplyTheme(UITheme.SelectedTheme);
            }catch(Exception){}
        }
    }

    public enum UIThemeObjectType
    {
        BackgroundColor1 = 0,
        BackgroundColor2 = 1,
        BackgroundColor3 = 2,
        BackgroundColor4 = 3,
        BackgroundColor5 = 4,
        FirstText = 5,
        SecondText = 6,
        ThirdText = 11,
        LinkText = 12,
        ButtonText = 7,
        PrimaryColorVector = 8,
        BackgroundImage = 9,
        LineRenderer = 10
    }
}