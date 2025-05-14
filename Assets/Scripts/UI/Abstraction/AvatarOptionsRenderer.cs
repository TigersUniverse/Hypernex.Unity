using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hypernex.CCK.Unity.Assets;
using Hypernex.Databasing.Objects;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Game.Avatar.FingerInterfacing;
using Hypernex.Networking.Messages;
using Hypernex.Tools;
using Hypernex.UI.Abstraction.AvatarControllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Renderer
{
    public class AvatarOptionsRenderer : UIRender, IRender<(AvatarMenu, AvatarParameters)>
    {
        private const string TOGGLE_CONTROL = "ToggleControl";
        private const string SLIDER_CONTROL = "SliderControl";
        private const string DROPDOWN_CONTROL = "DropdownControl";
        private const string AXIS_CONTROL = "2DAxisControl";
        private const string SUBMENU_CONTROL = "SubMenuControl";
        private const string RETURN_BUTTON = "ReturnControl";
        
        public RectTransform CubeHolder;
        public TMP_Text AvatarScaleLabel;
        public Slider AvatarScaleSlider;
        public TMP_Dropdown GestureIdentifierDropdown;
        public TMP_Dropdown ProfilesDropdown;
        public GameObject ProfilePanel;
        public TMP_InputField ProfileField;

        private List<IParameterControl> currentParameterControls = new List<IParameterControl>();
        private Queue<AvatarMenu> stackedMenus = new Queue<AvatarMenu>();
        private AvatarMenu rootMenu;
        private AvatarMenu current;
        private AvatarParameters globalParameters;
        private LocalAvatarCreator avatarCreator;
        
        public void Render((AvatarMenu, AvatarParameters) t)
        {
            bool diff = avatarCreator == null || avatarCreator == LocalPlayer.Instance.avatar;
            avatarCreator = LocalPlayer.Instance.avatar;
            stackedMenus.Clear();
            rootMenu = t.Item1;
            globalParameters = t.Item2;
            ShowMenu(rootMenu, true);
            RenderGestures();
            RenderProfiles(diff);
            if (!string.IsNullOrEmpty(avatarCreator.AvatarConfiguration.SelectedWeight))
                ProfilesDropdown.value = GetProfileDropwdownIndexFromProfileName(avatarCreator.AvatarConfiguration.SelectedWeight);
            AvatarScaleSlider.value = (float) Math.Round(LocalPlayer.Instance.Scale, 1);
        }

        public void Return()
        {
            AvatarMenu previous = stackedMenus.Dequeue();
            if (previous == null) previous = rootMenu;
            ShowMenu(previous, previous == rootMenu);
        }

        public void ShowMenu(AvatarMenu menu, bool root)
        {
            CubeHolder.ClearChildren();
            currentParameterControls.Clear();
            if(!root) CreateReturn();
            if(menu == null) return;
            foreach (AvatarControl avatarControl in menu.Controls)
            {
                bool validParam1 = TryGetParameter(globalParameters, avatarControl.TargetParameterIndex - 1,
                    out AvatarParameter avatarParameter1);
                bool validParam2 = TryGetParameter(globalParameters, avatarControl.TargetParameterIndex2 - 1,
                    out AvatarParameter avatarParameter2);
                switch (avatarControl.ControlType)
                {
                    case ControlType.Toggle:
                        if(!validParam1)
                        {
                            WarnControl(avatarControl);
                            continue;
                        }
                        currentParameterControls.Add(CreateToggle(avatarControl, avatarParameter1));
                        break;
                    case ControlType.Slider:
                        if(!validParam1)
                        {
                            WarnControl(avatarControl);
                            continue;
                        }
                        currentParameterControls.Add(CreateSlider(avatarControl, avatarParameter1));
                        break;
                    case ControlType.Dropdown:
                        if(!validParam1)
                        {
                            WarnControl(avatarControl);
                            continue;
                        }
                        currentParameterControls.AddRange(CreateDropdown(avatarControl, avatarParameter1));
                        break;
                    case ControlType.TwoDimensionalAxis:
                        if (!validParam1 || !validParam2)
                        {
                            WarnControl(avatarControl);
                            continue;
                        }
                        currentParameterControls.Add(CreateAxis(avatarControl, avatarParameter1, avatarParameter2));
                        continue;
                    case ControlType.SubMenu:
                        if (avatarControl.SubMenu == null)
                        {
                            WarnControl(avatarControl);
                            continue;
                        }
                        CreateSubMenu(avatarControl);
                        break;
                }
            }
            if (!root)
                stackedMenus.Enqueue(current);
            current = menu;
        }

        private bool TryGetParameter(AvatarParameters parameters, int index, out AvatarParameter avatarParameter)
        {
            if (index >= parameters.Parameters.Length || index < 0)
            {
                avatarParameter = null;
                return false;
            }
            avatarParameter = parameters.Parameters[index];
            return true;
        }
        
        private void CreateReturn()
        {
            ReturnButtonControl returnButtonControl = (ReturnButtonControl) Defaults.GetRenderer<AvatarOptionsRenderer>(RETURN_BUTTON);
            returnButtonControl.Render(this);
            CubeHolder.AddChild(returnButtonControl.transform);
        }
        
        private IParameterControl CreateToggle(AvatarControl avatarControl, AvatarParameter avatarParameter)
        {
            ToggleControl toggleControl = (ToggleControl) Defaults.GetRenderer<(AvatarControl,AvatarParameter)>(TOGGLE_CONTROL);
            toggleControl.Render((avatarControl, avatarParameter));
            CubeHolder.AddChild(toggleControl.transform);
            return toggleControl;
        }
        
        private IParameterControl CreateSlider(AvatarControl avatarControl, AvatarParameter avatarParameter)
        {
            SliderControl sliderControl = (SliderControl) Defaults.GetRenderer<(AvatarControl,AvatarParameter)>(SLIDER_CONTROL);
            sliderControl.Render((avatarControl, avatarParameter));
            CubeHolder.AddChild(sliderControl.transform);
            return sliderControl;
        }
        
        private IParameterControl[] CreateDropdown(AvatarControl avatarControl, AvatarParameter avatarParameter)
        {
            DropdownContainer dropdownContainer = (DropdownContainer) Defaults.GetRenderer<(AvatarControl,AvatarParameter)>(DROPDOWN_CONTROL);
            dropdownContainer.Render((avatarControl, avatarParameter));
            CubeHolder.AddChild(dropdownContainer.transform);
            return dropdownContainer.Dropdowns.Select(x => (IParameterControl) x).ToArray();
        }
        
        private IParameterControl CreateAxis(AvatarControl avatarControl, AvatarParameter avatarParameter1, AvatarParameter avatarParameter2)
        {
            AxisControl axisControl = (AxisControl) Defaults.GetRenderer<(AvatarControl,AvatarParameter,AvatarParameter)>(AXIS_CONTROL);
            axisControl.Render((avatarControl, avatarParameter1, avatarParameter2));
            CubeHolder.AddChild(axisControl.transform);
            return axisControl;
        }
        
        private void CreateSubMenu(AvatarControl avatarControl)
        {
            SubMenuControl subMenuControl = (SubMenuControl) Defaults.GetRenderer<(AvatarOptionsRenderer,AvatarControl)>(SUBMENU_CONTROL);
            subMenuControl.Render((this, avatarControl));
            CubeHolder.AddChild(subMenuControl.transform);
        }

        private void WarnControl(AvatarControl avatarControl) => Logger.CurrentLogger.Warn(
            $"[AVATAR] Could not create control {avatarControl.ControlName} because it has invalid parameter(s)/references!");
        
        private void RenderGestures()
        {
            GestureIdentifierDropdown.ClearOptions();
            (int?, List<TMP_Dropdown.OptionData>) r = GetGestureIdentifiers(avatarCreator.AvatarConfiguration);
            GestureIdentifierDropdown.options = r.Item2;
            GestureIdentifierDropdown.value = r.Item1 != null ? r.Item1.Value + 1 : 0;
        }
        
        private (int?, List<TMP_Dropdown.OptionData>) GetGestureIdentifiers(AvatarConfiguration avatarConfiguration)
        {
            int? found = null;
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>
            {
                new("default")
            };
            for (int i = 0; i < FingerCalibration.GestureIdentifiers.Count; i++)
            {
                IGestureIdentifier gestureIdentifier = FingerCalibration.GestureIdentifiers.ElementAt(i);
                options.Add(new TMP_Dropdown.OptionData(gestureIdentifier.Name));
                if (gestureIdentifier.Name.ToLower() == avatarConfiguration.GestureIdentifierOverride.ToLower())
                    found = i;
            }
            return (found, options);
        }
        
        public void ApplyGestureIdentifier(string gestureName)
        {
            if (gestureName.ToLower() == "default")
            {
                avatarCreator.AvatarConfiguration.GestureIdentifierOverride = String.Empty;
                avatarCreator.SaveAvatarConfiguration();
                return;
            }
            avatarCreator.AvatarConfiguration.GestureIdentifierOverride = gestureName;
            avatarCreator.SaveAvatarConfiguration();
        }

        public void ApplyGestureIdentifier(int dropdownIndex) =>
            ApplyGestureIdentifier(GestureIdentifierDropdown.options.ElementAt(dropdownIndex).text);
        
        private void RenderProfiles(bool setToZero = false)
        {
            int l = ProfilesDropdown.value;
            ProfilesDropdown.ClearOptions();
            ProfilesDropdown.options = GetProfiles();
            ProfilesDropdown.value = setToZero ? 0 : l;
        }
        
        private List<TMP_Dropdown.OptionData> GetProfiles()
        {
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>
            {
                new ("default")
            };
            if (avatarCreator.AvatarConfiguration == null)
                return options;
            foreach (string savedWeightsKey in avatarCreator.AvatarConfiguration.SavedWeights.Keys)
                options.Add(new(savedWeightsKey));
            return options;
        }
        
        public void ApplyProfile(string profileName)
        {
            if (profileName.ToLower() == "default")
            {
                avatarCreator.SetParameters(avatarCreator.DefaultWeights.ToArray());
                avatarCreator.AvatarConfiguration.SelectedWeight = String.Empty;
                avatarCreator.SaveAvatarConfiguration();
                return;
            }
            int profileIndex = GetProfileIndex(profileName);
            if(profileIndex < 0 || profileIndex > ProfilesDropdown.options.Count - 1) return;
            avatarCreator.SetParameters(avatarCreator.AvatarConfiguration.SavedWeights.Values.ElementAt(profileIndex));
            avatarCreator.AvatarConfiguration.SelectedWeight = GetProfileNameFromProfileIndex(profileIndex);
            avatarCreator.SaveAvatarConfiguration();
        }

        public void ApplyProfile(int dropdownIndex) =>
            ApplyProfile(ProfilesDropdown.options.ElementAt(dropdownIndex).text);

        private int GetProfileIndex(string profileName)
        {
            for (int i = 0; i < avatarCreator.AvatarConfiguration.SavedWeights.Count; i++)
            {
                string savedWeight = avatarCreator.AvatarConfiguration.SavedWeights.Keys.ElementAt(i);
                if(savedWeight.ToLower() != profileName.ToLower()) continue;
                return i;
            }
            return -1;
        }

        private string GetProfileNameFromProfileIndex(int profileIndex) =>
            avatarCreator.AvatarConfiguration.SavedWeights.Keys.ElementAt(profileIndex);

        private int GetProfileDropwdownIndexFromProfileName(string profileName)
        {
            for (int i = 0; i < ProfilesDropdown.options.Count; i++)
            {
                TMP_Dropdown.OptionData optionData = ProfilesDropdown.options.ElementAt(i);
                if(optionData.text.ToLower() != profileName.ToLower()) continue;
                return i;
            }
            return 0;
        }
        
        public void SubmitProfile()
        {
            string profileName = ProfileField.text;
            if(string.IsNullOrEmpty(profileName))
            {
                Logger.CurrentLogger.Error("[AVATAR] Invalid Profile Name!");
                return;
            }
            int profileIndex = GetProfileIndex(profileName);
            WeightedObjectUpdate[] weights = avatarCreator.GetAnimatorWeights(true, includeSaved:false).ToArray();
            if (profileIndex > -1)
                avatarCreator.AvatarConfiguration.SavedWeights[GetProfileNameFromProfileIndex(profileIndex)] = weights;
            else
                avatarCreator.AvatarConfiguration.SavedWeights.Add(profileName, weights);
            avatarCreator.SaveAvatarConfiguration();
            RenderProfiles();
            ProfilesDropdown.value = GetProfileDropwdownIndexFromProfileName(profileName);
            HideAddProfile();
        }

        public void RemoveProfile()
        {
            string profileName = ProfilesDropdown.options.ElementAt(ProfilesDropdown.value).text;
            if (profileName.ToLower() == "default")
            {
                Logger.CurrentLogger.Warn("[AVATAR] Cannot remove default profile!");
                return;
            }
            int profileIndex = GetProfileIndex(profileName);
            if (profileIndex <= -1)
            {
                Logger.CurrentLogger.Warn("[AVATAR] Could not find profile!");
                return;
            }
            avatarCreator.AvatarConfiguration.SavedWeights.Remove(GetProfileNameFromProfileIndex(profileIndex));
            avatarCreator.SaveAvatarConfiguration();
            RenderProfiles(true);
            avatarCreator.SetParameters(avatarCreator.DefaultWeights.ToArray());
        }

        public void SaveProfile()
        {
            string profileName = ProfilesDropdown.options.ElementAt(ProfilesDropdown.value).text;
            int profileIndex = GetProfileIndex(profileName);
            if (profileIndex <= -1)
            {
                Logger.CurrentLogger.Warn("[AVATAR] Could not find profile!");
                return;
            }
            WeightedObjectUpdate[] weights = avatarCreator.GetAnimatorWeights(true).ToArray();
            avatarCreator.AvatarConfiguration.SavedWeights[GetProfileNameFromProfileIndex(profileIndex)] = weights;
            avatarCreator.SaveAvatarConfiguration();
            RenderProfiles();
        }

        public void ShowAddProfile()
        {
            ProfileField.text = String.Empty;
            ProfilePanel.SetActive(true);
        }

        public void HideAddProfile() => ProfilePanel.SetActive(false);
        
        private bool IsVRTriggerPressed()
        {
            foreach (IBinding binding in LocalPlayer.Instance.Bindings)
            {
                if (binding.Trigger > 0.9f)
                    return true;
            }
            return false;
        }

        private void OnEnable()
        {
            foreach (IParameterControl parameterControl in currentParameterControls)
            {
                parameterControl.UpdateState();
            }
        }

        private void Update()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null ||
                !LocalPlayer.Instance.avatar.Calibrated || (GameInstance.FocusedInstance != null &&
                                                            GameInstance.FocusedInstance.World != null &&
                                                            !GameInstance.FocusedInstance.World.AllowScaling))
            {
                AvatarScaleSlider.gameObject.SetActive(false);
                return;
            }
            AvatarScaleSlider.gameObject.SetActive(true);
            AvatarScaleLabel.text = Math.Round(LocalPlayer.Instance.Scale, 1).ToString(CultureInfo.CurrentCulture);
            float lv = LocalPlayer.Instance.Scale;
            float v = (float) Math.Round(AvatarScaleSlider.value, 1);
            if(!LocalPlayer.IsVR || !IsVRTriggerPressed() && v != (float) Math.Round(lv, 1))
                LocalPlayer.Instance.Scale = v;
        }
    }
}