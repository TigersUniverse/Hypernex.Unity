using TMPro;
using UnityEngine.UI;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class LocalUI
    {
        public static void RegisterButtonClick(Item item, SandboxAction s)
        {
            Button b = item.t.gameObject.GetComponent<Button>();
            if(b != null)
                b.onClick.AddListener(s.ua);
        }

        public static void RemoveButtonClick(Item item, SandboxAction s)
        {
            Button b = item.t.gameObject.GetComponent<Button>();
            if(b != null)
                b.onClick.RemoveListener(s.ua);
        }

        public static void RemoveAllButtonClicks(Item item)
        {
            Button b = item.t.gameObject.GetComponent<Button>();
            if(b != null)
                b.onClick.RemoveAllListeners();
        }

        public static void SetText(Item item, string text)
        {
            TMP_Text tmpt = item.t.gameObject.GetComponent<TMP_Text>();
            if (tmpt != null)
                tmpt.text = text;
            Text t = item.t.gameObject.GetComponent<Text>();
            if (t != null)
                t.text = text;
        }
    }
}