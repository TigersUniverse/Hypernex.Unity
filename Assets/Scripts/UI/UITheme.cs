using System.Collections.Generic;
using System.Linq;
using Hypernex.Game;
using Hypernex.Tools;
using TMPro;
using UnityEngine;

namespace Hypernex.UI
{
    public class UITheme : MonoBehaviour
    {
        public static List<UITheme> UIThemes = new();
        public static UITheme SelectedTheme;

        public static UITheme GetUIThemeByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            foreach (UITheme uiTheme in new List<UITheme>(UIThemes))
            {
                if (uiTheme.ThemeName.ToLower() == name.ToLower())
                    return uiTheme;
            }
            return null;
        }
        
        public string ThemeName;

        public Sprite BackgroundImage;
        
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color PrimaryVectorColor;
    
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
            CursorTools.UpdateMouseIcon(!LocalPlayer.Instance.Dashboard.IsVisible, PrimaryVectorColor);
        }

        public UIButtonTheme GetButtonThemeFromButtonType(ButtonType type) =>
            ButtonThemes.FirstOrDefault(x => x.ButtonType == type);

        private void Start() => UIThemes.Add(this);
    }
}