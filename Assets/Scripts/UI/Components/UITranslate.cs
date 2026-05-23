using Hypernex.Tools;
using TMPro;
using UI.Abstraction;
using UnityEngine;

namespace Hypernex.UI.Components
{
    [RequireComponent(typeof(TMP_Text))]
    public class UITranslate : MonoBehaviour, ITranslateElement
    {
        public string Key;
        
        private TMP_Text text;
        private bool translated;

        private void CacheComponent()
        {
            text = GetComponent<TMP_Text>();
            if(TranslateCore.TranslateElements.Contains(this)) return;
            TranslateCore.TranslateElements.Add(this);
        }

        private void OnEnable()
        {
            CacheComponent();
            if(translated) return;
            Translate();
        }

        public void Translate()
        {
            if(text == null) CacheComponent();
            if(text == null) return;
            string s = TranslateCore.GetStaticTranslation(Key);
            if(string.IsNullOrEmpty(s)) return;
            text.text = s;
            translated = true;
        }
    }
}