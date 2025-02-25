using System;
using Hypernex.CCK.Unity;
using Hypernex.Configuration;
using Hypernex.UIActions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class ComponentToggleButton : MonoBehaviour
    {
        public SettingsPageManager SettingsPageManager;
        public SafetyType Safety;
        public TMP_Text Label;
        public Image Icon;

        public Color ActiveColor => UITheme.SelectedTheme.PrimaryColorTheme;
        public Color DisabledColor => new(0.2f, 0.2f, 0.2f, 1f);

        public void OnToggle()
        {
            switch (SettingsPageManager.SelectedSecurityType)
            {
                case 1:
                    ApplySecurityType(ref ConfigManager.SelectedConfigUser.FriendsAvatarComponents);
                    break;
                default:
                    ApplySecurityType(ref ConfigManager.SelectedConfigUser.AnyoneAvatarComponents);
                    break;
            }
            UpdateColors();
        }

        private void OnEnable() => UpdateColors();

        public void UpdateColors()
        {
            AllowedAvatarComponent allowedAvatarComponent;
            switch (SettingsPageManager.SelectedSecurityType)
            {
                case 1:
                    allowedAvatarComponent = ConfigManager.SelectedConfigUser.FriendsAvatarComponents;
                    break;
                default:
                    allowedAvatarComponent = ConfigManager.SelectedConfigUser.AnyoneAvatarComponents;
                    break;
            }
            bool value;
            switch (Safety)
            {
                case SafetyType.Scripting:
                    value = allowedAvatarComponent.Scripting;
                    break;
                case SafetyType.Physics:
                    value = allowedAvatarComponent.Physics;
                    break;
                case SafetyType.Audio:
                    value = allowedAvatarComponent.Audio;
                    break;
                case SafetyType.UI:
                    value = allowedAvatarComponent.UI;
                    break;
                case SafetyType.Light:
                    value = allowedAvatarComponent.Light;
                    break;
                case SafetyType.Particle:
                    value = allowedAvatarComponent.Particle;
                    break;
                default:
                    throw new Exception("Invalid SafetyType");
            }
            Label.font = UITheme.SelectedTheme.FirstLabelFont;
            Label.color = value ? ActiveColor : DisabledColor;
            Icon.color = value ? ActiveColor : DisabledColor;
        }

        private bool ApplySecurityType(ref AllowedAvatarComponent allowedAvatarComponent)
        {
            bool value;
            switch (Safety)
            {
                case SafetyType.Scripting:
                    value = !allowedAvatarComponent.Scripting;
                    allowedAvatarComponent.Scripting = value;
                    break;
                case SafetyType.Physics:
                    value = !allowedAvatarComponent.Physics;
                    allowedAvatarComponent.Physics = value;
                    break;
                case SafetyType.Audio:
                    value = !allowedAvatarComponent.Audio;
                    allowedAvatarComponent.Audio = value;
                    break;
                case SafetyType.UI:
                    value = !allowedAvatarComponent.UI;
                    allowedAvatarComponent.UI = value;
                    break;
                case SafetyType.Light:
                    value = !allowedAvatarComponent.Light;
                    allowedAvatarComponent.Light = value;
                    break;
                case SafetyType.Particle:
                    value = !allowedAvatarComponent.Particle;
                    allowedAvatarComponent.Particle = value;
                    break;
                default:
                    throw new Exception("Invalid SafetyType");
            }
            return value;
        }

        public enum SafetyType
        {
            Scripting,
            Physics,
            Audio,
            UI,
            Light,
            Particle
        }
    }
}