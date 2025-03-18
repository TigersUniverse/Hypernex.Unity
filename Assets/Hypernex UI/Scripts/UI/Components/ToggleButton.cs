using System;
using UnityEngine;

namespace Hypernex.UI.Components
{
    [RequireComponent(typeof(UIThemeObject))]
    public class ToggleButton : MonoBehaviour
    {
        public ButtonType EnabledTheme = ButtonType.Blue;
        public ButtonType DisabledTheme = ButtonType.Grey;
        public ToggleButton[] Family = Array.Empty<ToggleButton>();
        public bool DefaultState;

        private UIThemeObject uiThemeObject;
        
        private bool m_isOn;
        public bool isOn
        {
            get => m_isOn;
            set
            {
                m_isOn = value;
                Repaint();
            }
        }

        public void Select()
        {
            foreach (ToggleButton toggleButton in Family)
                toggleButton.isOn = false;
            isOn = true;
        }

        public void Toggle() => isOn = !isOn;
        
        private void Repaint()
        {
            uiThemeObject.ButtonType = m_isOn ? EnabledTheme : DisabledTheme;
            uiThemeObject.ApplyTheme(UITheme.SelectedTheme);
        }

        private void Start()
        {
            uiThemeObject = GetComponent<UIThemeObject>();
            isOn = DefaultState;
        }
    }
}