using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Hypernex.UI
{
    public class UITheme : MonoBehaviour
    {
        public static UITheme SelectedTheme;
        public string ThemeName;

        public Color PrimaryColor;
        public Color SecondaryColor;
    
        public Color PrimaryLabelColor;
        public TMP_FontAsset PrimaryFont;

        public Color SecondaryLabelColor;
        public TMP_FontAsset SecondaryFont;

        public Color PrimaryInputColor;
        public Color PrimaryInputTextColor;
        public TMP_FontAsset PrimaryInputFont;
        public Color SecondaryInputColor;
        public Color SecondaryInputTextColor;
        public TMP_FontAsset SecondaryInputFont;

        public List<UIButtonTheme> ButtonThemes;

        public void ApplyThemeToUI()
        {
            SelectedTheme = this;
            foreach (UIThemeObject UIThemeObject in FindObjectsOfType<UIThemeObject>(true))
            {
                UIThemeObject.ApplyTheme(this);
            }
        }

        public UIButtonTheme GetButtonThemeFromButtonType(ButtonType type) =>
            ButtonThemes.FirstOrDefault(x => x.ButtonType == type);
    }
}