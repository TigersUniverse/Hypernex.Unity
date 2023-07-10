using System;
using System.Collections.Generic;
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