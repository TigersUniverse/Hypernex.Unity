using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hypernex.Configuration;
using Hypernex.ExtendedTracking;
using Hypernex.Game;
using Hypernex.Tools;
using Hypernex.UI;
using Hypernex.UI.Templates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRCFaceTracking.Core.Models;
using Logger = Hypernex.CCK.Logger;

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

        public void OnGeneralSettings()
        {
            // AudioDevices
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
                WorldAudioSliderValueText.text = waRounded.ToString(CultureInfo.InvariantCulture);
                NoiseSuppressionToggle.isOn = ConfigManager.SelectedConfigUser.NoiseSuppression;
            }
            AllPanels.ForEach(x => x.SetActive(false));
            AudioPanel.SetActive(true);
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
                AudioCompressionSelection.value = (int) ConfigManager.SelectedConfigUser.AudioCompression;
            }
            AllPanels.ForEach(x => x.SetActive(false));
            UserPanel.SetActive(true);
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
        }
        
        public void SetSnapTurn(bool value)
        {
            if (ConfigManager.SelectedConfigUser == null)
                return;
            ConfigManager.SelectedConfigUser.UseSnapTurn = value;
            UseSnapTurnValue.text = ConfigManager.SelectedConfigUser.UseSnapTurn ? "Enabled" : "Disabled";
        }

        private UnifiedMutationConfig Mutations;

        public void OnFaceTrackingSettings()
        {
            FaceTrackingToggle.isOn = FaceTrackingManager.HasInitialized;
            FaceConfig.Clear();
            AllPanels.ForEach(x => x.SetActive(false));
            FaceTrackingPanel.SetActive(true);
            DisplayMutations();
        }

        private UnifiedMutationPanel CreateUnifiedMutationPanel(UnifiedMutation unifiedMutation, string n)
        {
            GameObject panel = DontDestroyMe.GetNotDestroyedObject("Templates").transform
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
                UnifiedMutationPanel gazePanel = CreateUnifiedMutationPanel(Mutations.GazeMutations, "Gaze Mutations");
                gazePanel.AddAction(() =>
                {
                    gazePanel.CopyTo(ref Mutations.GazeMutations);
                    SaveMutations();
                });
                UnifiedMutationPanel openPanel =
                    CreateUnifiedMutationPanel(Mutations.OpennessMutations, "Openness Mutations");
                openPanel.AddAction(() =>
                {
                    openPanel.CopyTo(ref Mutations.OpennessMutations);
                    SaveMutations();
                });
                UnifiedMutationPanel pupilPanel =
                    CreateUnifiedMutationPanel(Mutations.PupilMutations, "Pupil Mutations");
                pupilPanel.AddAction(() =>
                {
                    pupilPanel.CopyTo(ref Mutations.PupilMutations);
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
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
            }
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
                LocalPlayer.Instance.MicrophoneEnabled = v;
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
                WorldAudioSliderValueText.text = rounded.ToString(CultureInfo.InvariantCulture);
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
            OnGeneralSettings();
        }

        private void Update()
        {
            SelectedAudioLabel.text = "Selected Microphone: " + Mic.SelectedDevice;
        }
    }
}