using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hypernex.Configuration;
using Hypernex.ExtendedTracking;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Game.Avatar.FingerInterfacing;
using Hypernex.Tools;
using Hypernex.UI;
using Hypernex.UI.Templates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRCFaceTracking.Core.Models;

namespace Hypernex.UIActions
{
    public class SettingsPageManager : MonoBehaviour
    {
        public List<GameObject> AllPanels = new();

        public GameObject GeneralPanel;
        public TMP_Text SelectedAudioLabel;
        public TMP_Dropdown AudioDeviceSelection;
        public TMP_InputField DownloadThreadsInput;
        public TMP_InputField MMSCInput;

        public GameObject AudioPanel;
        public Slider VoicesBoostSlider;
        public TMP_Text VoicesBoostSliderValueText;
        public Slider WorldAudioSlider;
        public TMP_Text WorldAudioSliderValueText;
        public Toggle NoiseSuppressionToggle;

        public GameObject UserPanel;
        public TMP_Dropdown ThemeSelection;
        public TMP_Dropdown EmojiTypeSelection;
        public TMP_Dropdown AudioCompressionSelection;
        public TMP_Dropdown GestureSelection;

        public GameObject VRPanel;
        public TMP_Text UseSnapTurnValue;
        public Slider SnapTurnDegreeSlider;
        public TMP_Text SnapTurnDegreeValue;
        public Slider SmoothTurnSpeedSlider;
        public TMP_Text SmoothTurnSpeedValue;

        public GameObject FaceTrackingPanel;
        public Toggle FaceTrackingToggle;
        public GameObject RestartPanel;
        public DynamicScroll FaceConfig;

        public GameObject ConsolePanel;

        private Action lastVisiblePanel;

        public void OnGeneralSettings()
        {
            // AudioDevices
            Mic.Instance.RefreshDevices();
            AudioDeviceSelection.ClearOptions();
            List<TMP_Dropdown.OptionData> optionDatas = new();
            int selected = -1;
            int i = 0;
            foreach (string micDevice in Mic.Instance.Devices)
            {
                if (micDevice == Mic.SelectedDevice)
                    selected = i;
                optionDatas.Add(new TMP_Dropdown.OptionData(micDevice));
                i++;
            }
            AudioDeviceSelection.AddOptions(optionDatas);
            if (selected > -1)
                AudioDeviceSelection.value = selected;
            // Download Threads
            DownloadThreadsInput.text = ConfigManager.LoadedConfig.DownloadThreads.ToString();
            // MMSC
            MMSCInput.text = ConfigManager.LoadedConfig.MaxMemoryStorageCache.ToString();
            AllPanels.ForEach(x => x.SetActive(false));
            GeneralPanel.SetActive(true);
            lastVisiblePanel = OnGeneralSettings;
        }

        public void ApplyGeneralSettings()
        {
            try
            {
                int dtv = Convert.ToInt32(DownloadThreadsInput.text);
                ConfigManager.LoadedConfig.DownloadThreads = dtv;
            } catch(Exception){}
            try
            {
                int mmscv = Convert.ToInt32(MMSCInput.text);
                ConfigManager.LoadedConfig.MaxMemoryStorageCache = mmscv;
            } catch(Exception){}
            ConfigManager.SaveConfigToFile();
        }

        public void OnAudioSettings()
        {
            if (ConfigManager.SelectedConfigUser != null)
            {
                float vbRounded = (float) Math.Round(ConfigManager.SelectedConfigUser.VoicesBoost, 2);
                VoicesBoostSlider.value = vbRounded;
                VoicesBoostSliderValueText.text = vbRounded.ToString(CultureInfo.InvariantCulture) + " dB";
                float waRounded = (float) Math.Round(ConfigManager.SelectedConfigUser.WorldAudioVolume, 2);
                WorldAudioSlider.value = waRounded;
                WorldAudioSliderValueText.text = waRounded.ToString(CultureInfo.InvariantCulture) + " dB";
                NoiseSuppressionToggle.isOn = ConfigManager.SelectedConfigUser.NoiseSuppression;
                AudioCompressionSelection.value = (int) ConfigManager.SelectedConfigUser.AudioCompression;
            }
            AllPanels.ForEach(x => x.SetActive(false));
            AudioPanel.SetActive(true);
            lastVisiblePanel = OnAudioSettings;
        }

        public void OnUserSettings()
        {
            if(ConfigManager.SelectedConfigUser != null)
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
            AllPanels.ForEach(x => x.SetActive(false));
            UserPanel.SetActive(true);
            lastVisiblePanel = OnUserSettings;
        }

        public void OnVRSettings()
        {
            if (ConfigManager.SelectedConfigUser != null)
            {
                bool ust = ConfigManager.SelectedConfigUser.UseSnapTurn;
                UseSnapTurnValue.text = ust ? "Enabled" : "Disabled";
                float tdRounded = (float) Math.Round(ConfigManager.SelectedConfigUser.SnapTurnAngle, 2);
                SnapTurnDegreeValue.text = tdRounded.ToString(CultureInfo.InvariantCulture);
                SnapTurnDegreeSlider.value = tdRounded;
                float tsRounded = (float) Math.Round(ConfigManager.SelectedConfigUser.SmoothTurnSpeed, 2);
                SmoothTurnSpeedValue.text = tsRounded.ToString(CultureInfo.InvariantCulture);
                SmoothTurnSpeedSlider.value = tsRounded;
            }
            AllPanels.ForEach(x => x.SetActive(false));
            VRPanel.SetActive(true);
            lastVisiblePanel = OnVRSettings;
        }

        public void SetSnapTurn(bool value)
        {
            if (ConfigManager.SelectedConfigUser == null)
                return;
            ConfigManager.SelectedConfigUser.UseSnapTurn = value;
            UseSnapTurnValue.text = ConfigManager.SelectedConfigUser.UseSnapTurn ? "Enabled" : "Disabled";
        }

        public void CalibrateVR() => LocalPlayer.Instance.AlignVR(false);

        private UnifiedMutationConfig Mutations;

        public void OnFaceTrackingSettings()
        {
            FaceTrackingToggle.isOn = FaceTrackingManager.HasInitialized;
            FaceConfig.Clear();
            AllPanels.ForEach(x => x.SetActive(false));
            FaceTrackingPanel.SetActive(true);
            lastVisiblePanel = OnFaceTrackingSettings;
            DisplayMutations();
        }

        private UnifiedMutationPanel CreateUnifiedMutationPanel(MutationConfig unifiedMutation, string n)
        {
            GameObject panel = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("UnifiedMutationPanel").gameObject;
            GameObject newPanel = Instantiate(panel);
            RectTransform c = newPanel.GetComponent<RectTransform>();
            UnifiedMutationPanel p =  newPanel.GetComponent<UnifiedMutationPanel>();
            p.Render(unifiedMutation, n);
            FaceConfig.AddItem(c);
            return p;
        }

        private void SaveMutations()
        {
            FaceTrackingManager.SetSettings("Mutation", Mutations);
            ConfigManager.SaveConfigToFile();
        }

        public void DisplayMutations()
        {
            try
            {
                FaceConfig.Clear();
                Mutations =
                    FaceTrackingManager.GetSettings<UnifiedMutationConfig>("Mutation");
                UnifiedMutationPanel gazePanel = CreateUnifiedMutationPanel(Mutations.GazeMutationsConfig, "Gaze Mutations");
                gazePanel.AddAction(() =>
                {
                    gazePanel.CopyTo(ref Mutations.GazeMutationsConfig);
                    SaveMutations();
                });
                UnifiedMutationPanel openPanel =
                    CreateUnifiedMutationPanel(Mutations.OpennessMutationsConfig, "Openness Mutations");
                openPanel.AddAction(() =>
                {
                    openPanel.CopyTo(ref Mutations.OpennessMutationsConfig);
                    SaveMutations();
                });
                UnifiedMutationPanel pupilPanel =
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
                    UnifiedMutationPanel panel =
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
        
        public void OnConsoleSettings()
        {
            AllPanels.ForEach(x => x.SetActive(false));
            ConsolePanel.SetActive(true);
            lastVisiblePanel = OnConsoleSettings;
        }

        private void OnEnable()
        {
            AudioDeviceSelection.onValueChanged.RemoveAllListeners();
            AudioDeviceSelection.onValueChanged.AddListener(i =>
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
            });
            ThemeSelection.onValueChanged.RemoveAllListeners();
            ThemeSelection.onValueChanged.AddListener(i =>
            {
                if (ConfigManager.SelectedConfigUser == null)
                    return;
                string n = ThemeSelection.options.ElementAt(i).text;
                UITheme uiTheme = UITheme.GetUIThemeByName(n);
                if(uiTheme == null)
                    return;
                uiTheme.ApplyThemeToUI();
                ConfigManager.SelectedConfigUser.Theme = n;
            });
            EmojiTypeSelection.onValueChanged.RemoveAllListeners();
            EmojiTypeSelection.onValueChanged.AddListener(i =>
            {
                if(ConfigManager.SelectedConfigUser == null)
                    return;
                ConfigManager.SelectedConfigUser.EmojiType = i;
            });
            GestureSelection.onValueChanged.RemoveAllListeners();
            GestureSelection.onValueChanged.AddListener(i =>
            {
                if(ConfigManager.SelectedConfigUser == null)
                    return;
                IGestureIdentifier gestureIdentifier =
                    FingerCalibration.GetGestureIdentifierFromName(GestureSelection.options.ElementAt(i).text);
                if(gestureIdentifier == null) return;
                ConfigManager.SelectedConfigUser.GestureType = gestureIdentifier.Name;
                LocalPlayer.Instance.GestureIdentifier = gestureIdentifier;
            });
            AudioCompressionSelection.onValueChanged.RemoveAllListeners();
            AudioCompressionSelection.onValueChanged.AddListener(i =>
            {
                if(ConfigManager.SelectedConfigUser == null)
                    return;
                ConfigManager.SelectedConfigUser.AudioCompression = (AudioCompression) i;
            });
            VoicesBoostSlider.onValueChanged.RemoveAllListeners();
            VoicesBoostSlider.onValueChanged.AddListener(v =>
            {
                if(ConfigManager.SelectedConfigUser == null)
                    return;
                float rounded = (float) Math.Round(v, 2);
                ConfigManager.SelectedConfigUser.VoicesBoost = rounded;
                VoicesBoostSliderValueText.text = rounded.ToString(CultureInfo.InvariantCulture) + " dB";
            });
            WorldAudioSlider.onValueChanged.RemoveAllListeners();
            WorldAudioSlider.onValueChanged.AddListener(v =>
            {
                if(ConfigManager.SelectedConfigUser == null)
                    return;
                float rounded = (float) Math.Round(v, 2);
                ConfigManager.SelectedConfigUser.WorldAudioVolume = rounded;
                WorldAudioSliderValueText.text = rounded.ToString(CultureInfo.InvariantCulture) + " dB";
            });
            NoiseSuppressionToggle.onValueChanged.RemoveAllListeners();
            NoiseSuppressionToggle.onValueChanged.AddListener(v =>
            {
                if(ConfigManager.SelectedConfigUser == null)
                    return;
                ConfigManager.SelectedConfigUser.NoiseSuppression = v;
            });
            SnapTurnDegreeSlider.onValueChanged.RemoveAllListeners();
            SnapTurnDegreeSlider.onValueChanged.AddListener(v =>
            {
                if(ConfigManager.SelectedConfigUser == null)
                    return;
                float rounded = (float) Math.Round(v, 2);
                ConfigManager.SelectedConfigUser.SnapTurnAngle = rounded;
                SnapTurnDegreeValue.text = rounded.ToString(CultureInfo.InvariantCulture);
            });
            SmoothTurnSpeedSlider.onValueChanged.RemoveAllListeners();
            SmoothTurnSpeedSlider.onValueChanged.AddListener(v =>
            {
                if(ConfigManager.SelectedConfigUser == null)
                    return;
                float rounded = (float) Math.Round(v, 2);
                ConfigManager.SelectedConfigUser.SmoothTurnSpeed = rounded;
                SmoothTurnSpeedValue.text = rounded.ToString(CultureInfo.InvariantCulture);
            });
            FaceTrackingToggle.onValueChanged.RemoveAllListeners();
            FaceTrackingToggle.onValueChanged.AddListener(b =>
            {
                RestartPanel.SetActive(FaceTrackingManager.HasInitialized != b);
                ConfigManager.SelectedConfigUser.UseFacialTracking = b;
            });
            if(lastVisiblePanel != null)
                lastVisiblePanel.Invoke();
            else
                OnGeneralSettings();
        }

        private void Update()
        {
            SelectedAudioLabel.text = "Selected Microphone: " + Mic.SelectedDevice;
        }
    }
}