using System;
using System.Collections.Generic;
using Hypernex.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class KeyboardTemplate : MonoBehaviour
    {
        private static Dictionary<string, KeyboardTemplate> keyboards = new();

        public static KeyboardTemplate GetKeyboardTemplateByLanguage(string language)
        {
            foreach (KeyValuePair<string, KeyboardTemplate> keyboard in new Dictionary<string, KeyboardTemplate>(keyboards))
                if (keyboard.Key == language)
                    return keyboard.Value;
            return null;
        }
        
        public static bool IsInUse { get; private set; }

        public DashboardManager Dashboard;
        public TMP_Text Input;
        public Button Backspace;
        public Button Tab;
        public Button Caps;
        public Button Enter;
        public Button Space;
        public Button Cancel;
        public Button Submit;
        public Button Clear;
        public Button Paste;
        [HideInInspector] public bool IsCaps;

        private Dictionary<string, TMP_Text> cachedTexts = new();

        public void RequestInput(Action<string> onSubmit, Action onCancel = null)
        {
            if (IsInUse)
            {
                onCancel?.Invoke();
                return;
            }
            IsInUse = true;
            Input.text = String.Empty;
            bool previous = Dashboard.IsVisible;
            if (!Dashboard.IsVisible)
                Dashboard.ToggleDashboard(LocalPlayer.Instance);
            Submit.onClick.AddListener(() =>
            {
                onSubmit.Invoke(Input.text);
                Submit.onClick.RemoveAllListeners();
                if(Dashboard.IsVisible != previous)
                    Dashboard.ToggleDashboard(LocalPlayer.Instance);
                gameObject.SetActive(false);
                IsInUse = false;
            });
            Cancel.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                Cancel.onClick.RemoveAllListeners();
                if(Dashboard.IsVisible != previous)
                    Dashboard.ToggleDashboard(LocalPlayer.Instance);
                gameObject.SetActive(false);
                IsInUse = false;
            });
            gameObject.SetActive(true);
        }

        private void Start()
        {
            Backspace?.onClick.AddListener(() =>
            {
                if (Input.text.Length > 0)
                    Input.text = Input.text.Remove(Input.text.Length - 1);
            });
            Tab?.onClick.AddListener(() => Input.text += "\t");
            Caps?.onClick.AddListener(() => IsCaps = !IsCaps);
            Enter?.onClick.AddListener(() => Input.text += "\n");
            Space?.onClick.AddListener(() => Input.text += " ");
            Clear?.onClick.AddListener(() => Input.text = String.Empty);
            Paste?.onClick.AddListener(() => Input.text += GUIUtility.systemCopyBuffer);
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform t = transform.GetChild(i);
                if (t != Input.transform && t != Backspace?.transform && t != Tab?.transform && t != Caps?.transform &&
                    t != Enter?.transform && t != Space?.transform && t != Cancel?.transform &&
                    t != Submit?.transform && t != Clear?.transform && t != Paste?.transform && t.name != "input")
                {
                    Button b = t.gameObject.GetComponent<Button>();
                    TMP_Text text = t.GetChild(0).GetComponent<TMP_Text>();
                    b.onClick.AddListener(() => Input.text += text.text);
                    cachedTexts.Add(t.name, text);
                }
            }
            keyboards.Add(name.Split('-')[0], this);
            gameObject.SetActive(false);
        }

        private void Update()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform t = transform.GetChild(i);
                if (t != Input.transform && t != Backspace?.transform && t != Tab?.transform && t != Caps?.transform &&
                    t != Enter?.transform && t != Space?.transform && t != Cancel?.transform &&
                    t != Submit?.transform && t != Clear?.transform && t != Paste?.transform && t.name != "input")
                {
                    if (cachedTexts.ContainsKey(t.name))
                        cachedTexts[t.name].text = IsCaps ? t.name[1].ToString() : t.name[0].ToString();
                }
            }
        }
    }
}