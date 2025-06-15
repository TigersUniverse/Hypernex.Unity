using System;
using UnityEngine;
using UnityEngine.Events;

namespace Hypernex.UI.Components
{
    [RequireComponent(typeof(UIThemeObject))]
    public class ToggleButton : MonoBehaviour
    {
        public ButtonType EnabledTheme = ButtonType.Blue;
        public ButtonType DisabledTheme = ButtonType.Grey;
        public ToggleButton[] Family = Array.Empty<ToggleButton>();
        public bool DefaultState;
        public UnityEvent OnChange;

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
            RaiseEvents();
        }

        public void Toggle()
        {
            isOn = !isOn;
            RaiseEvents();
        }

        private void RaiseEvents() => OnChange.Invoke();
        
        private void Repaint()
        {
            if(uiThemeObject == null)
                uiThemeObject = GetComponent<UIThemeObject>();
            uiThemeObject.ButtonType = m_isOn ? EnabledTheme : DisabledTheme;
            uiThemeObject.ApplyTheme(UITheme.SelectedTheme);
        }

        private void Start()
        {
            if (DefaultState)
            {
                isOn = true;
                RaiseEvents();
            }
        }
    }
}