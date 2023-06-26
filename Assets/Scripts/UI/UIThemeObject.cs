using System;
using Hypernex.Game;
using Hypernex.UI.Templates;
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
                TMP_Dropdown dropdown = GetComponent<TMP_Dropdown>();
                if (dropdown != null)
                {
                    dropdown.colors = new ColorBlock
                    {
                        normalColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        selectedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        highlightedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        pressedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        colorMultiplier = 1
                    };
                    dropdown.itemText.color = theme.PrimaryLabelColor;
                    dropdown.captionText.color = theme.PrimaryLabelColor;
                }
                Toggle toggle = GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.colors = new ColorBlock
                    {
                        normalColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        selectedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        highlightedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        pressedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        colorMultiplier = 1
                    };
                }
                Scrollbar scrollbar = GetComponent<Scrollbar>();
                if (scrollbar != null)
                {
                    scrollbar.colors = new ColorBlock
                    {
                        normalColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        selectedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        highlightedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        pressedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryColor
                            : theme.SecondaryColor,
                        colorMultiplier = 1
                    };
                }
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
            else if (ThemeType is UIThemeObjectType.PrimaryInput or UIThemeObjectType.SecondaryInput)
            {
                TMP_InputField inputField = gameObject.GetComponent<TMP_InputField>();
                if (inputField != null)
                {
                    inputField.colors = new ColorBlock
                    {
                        normalColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryInputColor
                            : theme.SecondaryInputColor,
                        selectedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryInputColor
                            : theme.SecondaryInputColor,
                        highlightedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryInputColor
                            : theme.SecondaryInputColor,
                        pressedColor = ThemeType == UIThemeObjectType.PrimaryInput
                            ? theme.PrimaryInputColor
                            : theme.SecondaryInputColor,
                        colorMultiplier = 1
                    };
                    inputField.textComponent.color = ThemeType == UIThemeObjectType.PrimaryInput
                        ? theme.PrimaryInputTextColor
                        : theme.SecondaryInputTextColor;
                    inputField.textComponent.font = ThemeType == UIThemeObjectType.PrimaryInput
                        ? theme.PrimaryInputFont
                        : theme.SecondaryInputFont;
                    inputField.onSelect.RemoveAllListeners();
                    inputField.onSelect.AddListener(_ =>
                    {
                        if (!LocalPlayer.IsVR)
                            return;
                        KeyboardTemplate.GetKeyboardTemplateByLanguage("en").RequestInput(s => inputField.text = s);
                    });
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
        PrimaryVector,
        SecondaryVector,
        PrimaryText,
        SecondaryText,
        ButtonText,
        InverseVector,
        PrimaryInput,
        SecondaryInput
    }
}