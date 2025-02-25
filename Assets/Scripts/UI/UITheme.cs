using System.Collections.Generic;
using System.Linq;
using Hypernex.Game;
using Hypernex.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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
        
        [FormerlySerializedAs("PrimaryColor")] public Color BackgroundColor1;
        [FormerlySerializedAs("SecondaryColor")] public Color BackgroundColor2;
        public Color BackgroundColor3;
        public Color BackgroundColor4;
        public Color BackgroundColor5;
        [FormerlySerializedAs("PrimaryVectorColor")] public Color PrimaryColorTheme;

        [FormerlySerializedAs("PrimaryLabelColor")] public Color FirstLabelColor;
        [FormerlySerializedAs("PrimaryFont")] public TMP_FontAsset FirstLabelFont;
        [FormerlySerializedAs("SecondaryLabelColor")] public Color SecondLabelColor;
        [FormerlySerializedAs("SecondaryFont")] public TMP_FontAsset SecondaryLabelFont;
        public Color ThirdLabelColor;
        public TMP_FontAsset ThirdLabelFont;
        public Color LinkLabelColor;
        public TMP_FontAsset LinkLabelFont;

        public Sprite InfoSprite;
        public Sprite WarningSprite;
        public Sprite ErrorSprite;

        public List<UIButtonTheme> ButtonThemes;

        public void ApplyThemeToUI(bool fromDebug = false)
        {
            SelectedTheme = this;
            foreach (UIThemeObject uiThemeObject in FindObjectsByType<UIThemeObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                uiThemeObject.ApplyTheme(this);
            }
            if (fromDebug) return;
            CursorTools.UpdateMouseIcon(!LocalPlayer.Instance.Dashboard.IsVisible, PrimaryColorTheme);
            Init.Instance.OutlineMaterial.color = PrimaryColorTheme;
        }

        public UIButtonTheme GetButtonThemeFromButtonType(ButtonType type) =>
            ButtonThemes.FirstOrDefault(x => x.ButtonType == type);

        private void Start() => UIThemes.Add(this);
    }
}