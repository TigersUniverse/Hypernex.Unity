using Hypernex.Tools;
using TMPro;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Game
{
    [RequireComponent(typeof(TMP_Text))]
    public class Translate : MonoBehaviour
    {
        public string SourceLanguage = "auto";
        public bool OverrideLanguage;
        public string ToLanguage;
        
        private TMP_Text text;
        private TranslateCore translateCore = new TranslateCore();

        private async void TranslateText()
        {
            string input = text.text;
            string result = await translateCore.GetTranslation(input, SourceLanguage,
                OverrideLanguage ? ToLanguage : TranslateCore.ClientLanguage);
            if (result == null)
            {
                Logger.CurrentLogger.Warn($"Cannot translate {input}");
                return;
            }
            text.text = result;
        }
        
        private void OnEnable()
        {
            text = GetComponent<TMP_Text>();
            TranslateText();
        }

        private void OnDestroy()
        {
            translateCore?.Dispose();
        }
    }
}