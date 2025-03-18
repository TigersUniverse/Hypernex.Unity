using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.Configuration;
using Hypernex.ExtendedTracking;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Game.Avatar.FingerInterfacing;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using Hypernex.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRCFaceTracking.Core.Models;

namespace Hypernex.UI.Pages
{
    public class HomePage : UIPage
    {
        [Header("Settings Sub-SubPages")] public GameObject[] SettingsPages;
        [Header("Audio")]
        public TMP_Dropdown AudioDeviceSelection;
        public Slider VoicesBoostSlider;
        public TMP_Text VoicesBoostSliderValueText;
        public Slider WorldAudioSlider;
        public TMP_Text WorldAudioSliderValueText;
        public Slider AvatarAudioSlider;
        public TMP_Text AvatarAudioSliderValueText;
        public Toggle NoiseSuppressionToggle;
        [Header("User")]
        public TMP_Dropdown ThemeSelection;
        public TMP_Dropdown EmojiTypeSelection;
        public TMP_Dropdown GestureSelection;
        [Header("VR")]
        public ToggleButton SnapTurnButton;
        public ToggleButton SmoothTurnButton;
        public Slider SnapTurnDegreeSlider;
        public TMP_Text SnapTurnDegreeValue;
        public Slider SmoothTurnSpeedSlider;
        public TMP_Text SmoothTurnSpeedValue;
        [Header("Security")]
        public List<ToggleButton> ComponentToggleButtons = new();
        public int SelectedSecurityType;
        public Toggle TrustedURLsToggle;
        [Header("Face Tracking")]
        public Toggle FaceTrackingToggle;
        public GameObject RestartPanel;
        public GameObject OptionsPanel;
        public GameObject MutationsPanel;
        public RectTransform MutationsListTransform;
        public GameObject CamerasPanel;
        public RawImage Eyes;
        public RawImage Face;
        private UnifiedMutationConfig Mutations;

        #region Settings

        private void RefreshSettings()
        {
            if(ConfigManager.SelectedConfigUser == null)
                return;
            InitializeAudioSettings();
            InitializeUserSettings();
            InitializeVRSettings();
            InitializeSecuritySettings();
            InitializeFaceTrackingSettings();
        }

        public void FocusSettingsPage(int index)
        {
            foreach (GameObject settingsPage in SettingsPages)
                settingsPage.SetActive(false);
            SettingsPages[index].SetActive(true);
        }

        private void InitializeAudioSettings()
        {
            Mic.Instance.RefreshDevices();
            AudioDeviceSelection.ClearOptions();
            List<TMP_Dropdown.OptionData> optionDatas = new();
            int selected = -1;
            if (!string.IsNullOrEmpty(ConfigManager.LoadedConfig.SelectedMicrophone) &&
                Mic.Instance.Devices.Contains(ConfigManager.LoadedConfig.SelectedMicrophone))
                selected = Mic.Instance.Devices.IndexOf(ConfigManager.LoadedConfig.SelectedMicrophone);
            int i = 0;
            foreach (string micDevice in Mic.Instance.Devices)
            {
                if (selected < 0 && micDevice == Mic.SelectedDevice)
                    selected = i;
                optionDatas.Add(new TMP_Dropdown.OptionData(micDevice));
                i++;
            }
            AudioDeviceSelection.AddOptions(optionDatas);
            if (selected < 0) selected = 0;
            if(optionDatas.Count <= 0) optionDatas.Add(new TMP_Dropdown.OptionData("No Microphone!"));
            AudioDeviceSelection.value = selected;
            VoicesBoostSlider.value = ConfigManager.SelectedConfigUser.VoicesBoost;
            WorldAudioSlider.value = ConfigManager.SelectedConfigUser.WorldAudioVolume;
            AvatarAudioSlider.value = ConfigManager.SelectedConfigUser.AvatarAudioVolume;
            NoiseSuppressionToggle.isOn = ConfigManager.SelectedConfigUser.NoiseSuppression;
        }

        private void InitializeUserSettings()
        {
            ThemeSelection.ClearOptions();
            List<TMP_Dropdown.OptionData> optionDatas = new();
            foreach (UITheme uiTheme in new List<UITheme>(UITheme.UIThemes))
                optionDatas.Add(new TMP_Dropdown.OptionData(uiTheme.ThemeName));
            ThemeSelection.options = optionDatas;
            for (int i = 0; i < ThemeSelection.options.Count; i++)
            {
                TMP_Dropdown.OptionData optionData = ThemeSelection.options[i];
                if (optionData.text.ToLower() == UITheme.SelectedTheme.ThemeName.ToLower())
                    ThemeSelection.value = i;
            }
            EmojiTypeSelection.value = ConfigManager.SelectedConfigUser.EmojiType;
            List<TMP_Dropdown.OptionData> gestureOptions = new List<TMP_Dropdown.OptionData>();
            FingerCalibration.GestureIdentifiers.ForEach(gestureIdentifier => gestureOptions.Add(
                new TMP_Dropdown.OptionData
                {
                    text = gestureIdentifier.Name
                }));
            GestureSelection.options = gestureOptions;
            IGestureIdentifier gestureIdentifier =
                FingerCalibration.GetGestureIdentifierFromName(ConfigManager.SelectedConfigUser.GestureType) ??
                LocalPlayer.Instance.GestureIdentifier;
            int gestureIndex = FingerCalibration.GetGestureIndex(gestureIdentifier);
            if (gestureIndex < 0) gestureIndex = 0;
            GestureSelection.value = gestureIndex;
        }

        private void InitializeVRSettings()
        {
            float tdRounded = (float) Math.Round(ConfigManager.SelectedConfigUser.SnapTurnAngle, 2);
            SnapTurnDegreeValue.text = tdRounded.ToString(CultureInfo.InvariantCulture);
            SnapTurnDegreeSlider.value = tdRounded;
            float tsRounded = (float) Math.Round(ConfigManager.SelectedConfigUser.SmoothTurnSpeed, 2);
            SmoothTurnSpeedValue.text = tsRounded.ToString(CultureInfo.InvariantCulture);
            SmoothTurnSpeedSlider.value = tsRounded;
            if(ConfigManager.SelectedConfigUser.UseSnapTurn)
                SnapTurnButton.Select();
            else
                SmoothTurnButton.Select();
        }

        private void InitializeSecuritySettings()
        {
            UpdateComponentSecurityToggles();
            TrustedURLsToggle.isOn = ConfigManager.LoadedConfig.UseTrustedURLs;
        }

        private void InitializeFaceTrackingSettings()
        {
            FaceTrackingToggle.isOn = FaceTrackingManager.HasInitialized;
            MutationsListTransform.ClearChildren();
            if (FaceTrackingManager.HasInitialized)
            {
                RestartPanel.SetActive(false);
                OptionsPanel.SetActive(true);
                try
                {
                    MutationsListTransform.ClearChildren();
                    Mutations =
                        FaceTrackingManager.GetSettings<UnifiedMutationConfig>("Mutations");
                    UnifiedMutationRender gazePanel = CreateUnifiedMutationPanel(Mutations.GazeMutationsConfig, "Gaze Mutations");
                    gazePanel.AddAction(() =>
                    {
                        gazePanel.CopyTo(ref Mutations.GazeMutationsConfig);
                        SaveMutations();
                    });
                    UnifiedMutationRender openPanel =
                        CreateUnifiedMutationPanel(Mutations.OpennessMutationsConfig, "Openness Mutations");
                    openPanel.AddAction(() =>
                    {
                        openPanel.CopyTo(ref Mutations.OpennessMutationsConfig);
                        SaveMutations();
                    });
                    UnifiedMutationRender pupilPanel =
                        CreateUnifiedMutationPanel(Mutations.PupilMutationsConfig, "Pupil Mutations");
                    pupilPanel.AddAction(() =>
                    {
                        pupilPanel.CopyTo(ref Mutations.PupilMutationsConfig);
                        SaveMutations();
                    });
                    if (Mutations.ShapeMutations == null)
                        return;
                    for (int i = 0; i < Mutations.ShapeMutations.Length; i++)
                    {
                        int g = i;
                        UnifiedMutationRender panel =
                            CreateUnifiedMutationPanel(Mutations.ShapeMutations[i], "Shape Mutation " + i);
                        panel.AddAction(() =>
                        {
                            panel.CopyTo(ref Mutations.ShapeMutations[g]);
                            SaveMutations();
                        });
                    }
                }
                catch (Exception e){}
            }
            else
                RestartPanel.SetActive(FaceTrackingToggle.isOn);
            if (!FaceTrackingManager.HasInitialized)
            {
                MutationsPanel.SetActive(false);
                CamerasPanel.SetActive(false);
            }
        }
        
        public void OnAudioDeviceSelection(int i)
        {
            bool v = LocalPlayer.Instance.MicrophoneEnabled;
            LocalPlayer.Instance.MicrophoneEnabled = false;
            string device = Mic.Instance.Devices.ElementAt(i);
            if (!string.IsNullOrEmpty(device))
            {
                ConfigManager.LoadedConfig.SelectedMicrophone = device;
                ConfigManager.SaveConfigToFile();
            }
            LocalPlayer.Instance.MicrophoneEnabled = true;
            if(!v)
                LocalPlayer.Instance.MicrophoneEnabled = false;
        }

        public void OnVoicesBoostSlider(float v)
        {
            if(ConfigManager.SelectedConfigUser == null)
                return;
            float rounded = (float) Math.Round(v, 2);
            VoicesBoostSliderValueText.text = rounded.ToString(CultureInfo.InvariantCulture) + " dB";
            ConfigManager.SelectedConfigUser.VoicesBoost = rounded;
        }

        public void OnWorldAudioSlider(float v)
        {
            if(ConfigManager.SelectedConfigUser == null)
                return;
            float rounded = (float) Math.Round(v, 2);
            WorldAudioSliderValueText.text = rounded.ToString(CultureInfo.InvariantCulture) + " dB";
            ConfigManager.SelectedConfigUser.WorldAudioVolume = rounded;
        }
        
        public void OnAvatarAudioSlider(float v)
        {
            if(ConfigManager.SelectedConfigUser == null)
                return;
            float rounded = (float) Math.Round(v, 2);
            AvatarAudioSliderValueText.text = rounded.ToString(CultureInfo.InvariantCulture) + " dB";
            ConfigManager.SelectedConfigUser.AvatarAudioVolume = rounded;
        }
        
        public void OnNoiseSuppressionToggle(bool v)
        {
            if(ConfigManager.SelectedConfigUser == null)
                return;
            ConfigManager.SelectedConfigUser.NoiseSuppression = v;
        }

        public void OnThemeSelection(int i)
        {
            if (ConfigManager.SelectedConfigUser == null)
                return;
            string n = ThemeSelection.options.ElementAt(i).text;
            UITheme uiTheme = UITheme.GetUIThemeByName(n);
            if(uiTheme == null)
                return;
            uiTheme.ApplyThemeToUI();
            ConfigManager.SelectedConfigUser.Theme = n;
        }

        public void OnEmojiTypeSelection(int i)
        {
            if(ConfigManager.SelectedConfigUser == null)
                return;
            ConfigManager.SelectedConfigUser.EmojiType = i;
        }

        public void OnGestureSelection(int i)
        {
            if(ConfigManager.SelectedConfigUser == null)
                return;
            IGestureIdentifier gestureIdentifier =
                FingerCalibration.GetGestureIdentifierFromName(GestureSelection.options.ElementAt(i).text);
            if(gestureIdentifier == null) return;
            ConfigManager.SelectedConfigUser.GestureType = gestureIdentifier.Name;
        }

        public void OnSnapTurnSlider(float v)
        {
            if(ConfigManager.SelectedConfigUser == null)
                return;
            float rounded = (float) Math.Round(v, 2);
            ConfigManager.SelectedConfigUser.SnapTurnAngle = rounded;
            SnapTurnDegreeValue.text = rounded.ToString(CultureInfo.InvariantCulture);
        }
        
        public void SetSnapTurn(bool value)
        {
            if (ConfigManager.SelectedConfigUser == null)
                return;
            ConfigManager.SelectedConfigUser.UseSnapTurn = value;
            if(value)
                SnapTurnButton.Select();
            else
                SmoothTurnButton.Select();
        }

        public void CalibrateVR() => LocalPlayer.Instance.AlignVR(false);

        public void OnSmoothTurnSlider(float v)
        {
            if(ConfigManager.SelectedConfigUser == null)
                return;
            float rounded = (float) Math.Round(v, 2);
            ConfigManager.SelectedConfigUser.SmoothTurnSpeed = rounded;
            SmoothTurnSpeedValue.text = rounded.ToString(CultureInfo.InvariantCulture);
        }

        public void AnyoneSecuritySelected()
        {
            SelectedSecurityType = 0;
            UpdateComponentSecurityToggles();
        }
        
        public void FriendSecuritySelected()
        {
            SelectedSecurityType = 1;
            UpdateComponentSecurityToggles();
        }

        public void ToggleSecurityComponent(int security)
        {
            ToggleButton toggleButton = ComponentToggleButtons[security];
            switch (SelectedSecurityType)
            {
                case 1:
                    toggleButton.isOn = ApplySecurityType(security, ref ConfigManager.SelectedConfigUser.FriendsAvatarComponents);
                    break;
                default:
                    toggleButton.isOn = ApplySecurityType(security, ref ConfigManager.SelectedConfigUser.AnyoneAvatarComponents);
                    break;
            }
        }

        public void OnTrustedURLToggle(bool v)
        {
            if (ConfigManager.LoadedConfig == null)
                return;
            ConfigManager.LoadedConfig.UseTrustedURLs = v;
        }

        public void OnFaceTrackingToggle(bool b)
        {
            RestartPanel.SetActive(FaceTrackingManager.HasInitialized != b);
            if (!b)
            {
                MutationsPanel.SetActive(false);
                CamerasPanel.SetActive(false);
                OptionsPanel.SetActive(false);
            }
            else if (FaceTrackingManager.HasInitialized)
            {
                MutationsPanel.SetActive(true);
                CamerasPanel.SetActive(false);
                OptionsPanel.SetActive(true);
            }
            ConfigManager.SelectedConfigUser.UseFacialTracking = b;
        }

        public void ShowMutations()
        {
            if(!FaceTrackingManager.HasInitialized) return;
            CamerasPanel.SetActive(false);
            MutationsPanel.SetActive(true);
        }

        public void ShowCameras()
        {
            if(!FaceTrackingManager.HasInitialized) return;
            CamerasPanel.SetActive(true);
            MutationsPanel.SetActive(false);
        }
        
        public void RestartFaceTracking() => FaceTrackingManager.Restart();
        
        private void UpdateComponentSecurityToggles()
        {
            AllowedAvatarComponent allowedAvatarComponent;
            switch (SelectedSecurityType)
            {
                case 1:
                    allowedAvatarComponent = ConfigManager.SelectedConfigUser.FriendsAvatarComponents;
                    break;
                default:
                    allowedAvatarComponent = ConfigManager.SelectedConfigUser.AnyoneAvatarComponents;
                    break;
            }
            for (int i = 0; i < ComponentToggleButtons.Count; i++)
            {
                ToggleButton toggleButton = ComponentToggleButtons[i];
                bool value;
                switch (i)
                {
                    case 0:
                        value = allowedAvatarComponent.Scripting;
                        break;
                    case 1:
                        value = allowedAvatarComponent.Physics;
                        break;
                    case 2:
                        value = allowedAvatarComponent.Audio;
                        break;
                    case 3:
                        value = allowedAvatarComponent.UI;
                        break;
                    case 4:
                        value = allowedAvatarComponent.Light;
                        break;
                    case 5:
                        value = allowedAvatarComponent.Particle;
                        break;
                    default:
                        throw new Exception("Invalid SafetyType");
                }
                toggleButton.isOn = value;
            }
        }
        
        private bool ApplySecurityType(int safety, ref AllowedAvatarComponent allowedAvatarComponent)
        {
            bool value;
            switch (safety)
            {
                case 0:
                    value = !allowedAvatarComponent.Scripting;
                    allowedAvatarComponent.Scripting = value;
                    break;
                case 1:
                    value = !allowedAvatarComponent.Physics;
                    allowedAvatarComponent.Physics = value;
                    break;
                case 2:
                    value = !allowedAvatarComponent.Audio;
                    allowedAvatarComponent.Audio = value;
                    break;
                case 3:
                    value = !allowedAvatarComponent.UI;
                    allowedAvatarComponent.UI = value;
                    break;
                case 4:
                    value = !allowedAvatarComponent.Light;
                    allowedAvatarComponent.Light = value;
                    break;
                case 5:
                    value = !allowedAvatarComponent.Particle;
                    allowedAvatarComponent.Particle = value;
                    break;
                default:
                    throw new Exception("Invalid SafetyType");
            }
            return value;
        }
        
        private UnifiedMutationRender CreateUnifiedMutationPanel(MutationConfig unifiedMutation, string n)
        {
            IRender<(MutationConfig, string)> newPanel =
                Defaults.GetRenderer<(MutationConfig, string)>("UnifiedMutationPanel");
            RectTransform c = newPanel.GetComponent<RectTransform>();
            newPanel.Render((unifiedMutation, n));
            MutationsListTransform.AddChild(c);
            return (UnifiedMutationRender) newPanel;
        }
        
        private void SaveMutations()
        {
            FaceTrackingManager.SetSettings("Mutations", Mutations);
            ConfigManager.SaveConfigToFile();
        }
        
        private void UpdateCameras()
        {
            if (!CamerasPanel.activeSelf || VisibleSubPage != 1 || !FaceTrackingManager.HasInitialized) return;
            FaceTrackingManager.SetCameraTextures(ref Eyes, ref Face);
        }
        
        #endregion
        
        private void OnEnable()
        {
            RefreshSettings();
        }

        private void Update()
        {
            UpdateCameras();
        }
    }
}