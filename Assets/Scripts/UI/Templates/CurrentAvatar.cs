using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Databasing.Objects;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Game.Avatar.FingerInterfacing;
using Hypernex.Networking.Messages;
using Hypernex.UIActions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Templates
{
    public class CurrentAvatar : MonoBehaviour
    {
        public static CurrentAvatar Instance { get; internal set; }
        
        public LoginPageTopBarButton CurrentAvatarPage;
        public DashboardManager DashboardManager;
        public TMP_Text AvatarNameLabel;
        public TMP_Text AvatarScaleLabel;
        public Slider AvatarScaleSlider;
        public TMP_Dropdown GestureIdentifierDropdown;
        public TMP_Dropdown ProfilesDropdown;
        public DynamicScroll ParameterButtons;
        public List<ParameterTemplate> ParameterTemplates = new();
        public GameObject ProfilePanel;
        public TMP_InputField ProfileField;

        private LocalAvatarCreator avatarCreator;

        public void Render(LocalAvatarCreator a)
        {
            CurrentAvatarPage.Show();
            // Don't render the same avatar
            if(avatarCreator == a) return;
            avatarCreator = a;
            ParameterButtons.Clear();
            AvatarNameLabel.text = "Current Avatar: " + LocalPlayer.Instance.avatarMeta.Name;
            if(a.Avatar.Parameters != null)
            {
                foreach (AvatarParameter avatarParameter in a.Avatar.Parameters.Parameters)
                {
                    foreach (AnimatorPlayable animatorPlayable in a.AnimatorPlayables)
                    {
                        if (animatorPlayable.AnimatorControllerParameters.Count(x =>
                                x.name == avatarParameter.ParameterName) <=
                            0) continue;
                        AnimatorControllerParameter literalParameter =
                            animatorPlayable.AnimatorControllerParameters.First(x =>
                                x.name == avatarParameter.ParameterName);
                        CreateParameterButton(animatorPlayable, avatarParameter.ParameterName, literalParameter.type,
                            avatarParameter.ParameterType);
                    }
                }
            }
            RenderProfiles(true);
            if (!string.IsNullOrEmpty(a.AvatarConfiguration.SelectedWeight))
                ProfilesDropdown.value = GetProfileDropwdownIndexFromProfileName(a.AvatarConfiguration.SelectedWeight);
            RenderGestures();
        }

        private void RenderProfiles(bool setToZero = false)
        {
            ProfilesDropdown.onValueChanged.RemoveAllListeners();
            ProfilesDropdown.ClearOptions();
            ProfilesDropdown.options = GetProfiles();
            if(setToZero)
                ProfilesDropdown.value = 0;
            ProfilesDropdown.onValueChanged.AddListener(ApplyProfile);
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

        private void RenderGestures()
        {
            GestureIdentifierDropdown.onValueChanged.RemoveAllListeners();
            GestureIdentifierDropdown.ClearOptions();
            (int?, List<TMP_Dropdown.OptionData>) r = GetGestureIdentifiers(avatarCreator.AvatarConfiguration);
            GestureIdentifierDropdown.options = r.Item2;
            GestureIdentifierDropdown.value = r.Item1 != null ? r.Item1.Value + 1 : 0;
            GestureIdentifierDropdown.onValueChanged.AddListener(ApplyGestureIdentifier);
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

        private bool IsVRTriggerPressed()
        {
            foreach (IBinding binding in LocalPlayer.Instance.Bindings)
            {
                if (binding.Trigger > 0.9f)
                    return true;
            }
            return false;
        }

        public void RefreshAvatar(bool lpi)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            SizeAvatar(1f);
            if(lpi)
                LocalPlayer.Instance.RefreshAvatar(true);
            AvatarScaleSlider.value = LocalPlayer.Instance.transform.localScale.y;
            if(lpi)
                LoginPageTopBarButton.Show("Home");
        }

        public void SizeAvatar(float v)
        {
            AvatarScaleSlider.value = v;
            LocalPlayer.Instance.transform.localScale = new Vector3(v, v, v);
            Vector3 lp = LocalPlayer.Instance.transform.position;
            float scaleUp = DashboardManager.OpenedPosition.y + (v - DashboardManager.OpenedScale.y);
            float scaleDown = DashboardManager.OpenedBounds.min.y + v/2;
            LocalPlayer.Instance.transform.position = new Vector3(lp.x, v >= DashboardManager.OpenedScale.y ? scaleUp : scaleDown, lp.z);
            LocalPlayer.Instance.Dashboard.PositionDashboard(LocalPlayer.Instance);
        }

        private void CreateParameterButton(AnimatorPlayable animatorPlayable, string parameterName,
            AnimatorControllerParameterType literal, AnimatorControllerParameterType t)
        {
            GameObject parameterButton = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("ParameterSelect").gameObject;
            GameObject newParameterButton = Instantiate(parameterButton);
            Button b = newParameterButton.GetComponent<Button>();
            b.onClick.AddListener(() =>
            {
                ParameterTemplates.ForEach(x => x.gameObject.SetActive(false));
                ParameterTemplate pt = ParameterTemplates.First(x => x.ParameterType == t);
                pt.Render(animatorPlayable, parameterName, literal);
                pt.gameObject.SetActive(true);
            });
            newParameterButton.transform.GetChild(0).GetComponent<TMP_Text>().text = parameterName;
            RectTransform c = newParameterButton.GetComponent<RectTransform>();
            ParameterButtons.AddItem(c);
        }

        public void SubmitProfile()
        {
            string profileName = ProfileField.text;
            if(string.IsNullOrEmpty(profileName))
            {
                Logger.CurrentLogger.Error("Invalid Profile Name!");
                return;
            }
            int profileIndex = GetProfileIndex(profileName);
            WeightedObjectUpdate[] weights = avatarCreator.GetAnimatorWeights(true).ToArray();
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
                Logger.CurrentLogger.Warn("Cannot remove default profile!");
                return;
            }
            int profileIndex = GetProfileIndex(profileName);
            if (profileIndex <= -1)
            {
                Logger.CurrentLogger.Warn("Could not find profile!");
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
                Logger.CurrentLogger.Warn("Could not find profile!");
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
            // Doesn't matter which dimension, they should always be uniform
            AvatarScaleLabel.text = "Avatar Scale: " + Math.Round(AvatarScaleSlider.value, 1);
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            float lv = LocalPlayer.Instance.transform.localScale.y;
            float v = (float) Math.Round(AvatarScaleSlider.value, 1);
            if(!LocalPlayer.IsVR || !IsVRTriggerPressed() && v != (float) Math.Round(lv, 1))
                SizeAvatar(v);
        }
    }
}