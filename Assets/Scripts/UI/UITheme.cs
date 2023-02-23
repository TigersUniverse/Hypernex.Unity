using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UITheme : MonoBehaviour
{
    public string ThemeName;

    public Color PrimaryColor;
    public Color SecondaryColor;
    
    public Color PrimaryLabelColor;
    public TMP_FontAsset PrimaryFont;

    public Color SecondaryLabelColor;
    public TMP_FontAsset SecondaryFont;

    public List<UIButtonTheme> ButtonThemes;

    public void ApplyThemeToUI()
    {
        foreach (UIThemeObject UIThemeObject in FindObjectsOfType<UIThemeObject>(true))
        {
            UIThemeObject.ApplyTheme(this);
        }
    }

    public UIButtonTheme GetButtonThemeFromButtonType(ButtonType type) =>
        ButtonThemes.FirstOrDefault(x => x.ButtonType == type);
}