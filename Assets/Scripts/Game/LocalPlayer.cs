using System.Collections.Generic;
using System.IO;
using Adrenak.UniMic;
using Hypernex.Configuration;
// ReSharper disable once RedundantUsingDirective
using Hypernex.Game.Bindings;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Hypernex.UI;
using Nexport;
using OpusDotNet;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using Application = OpusDotNet.Application;
using Logger = Hypernex.Logging.Logger;

namespace Hypernex.Game
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class LocalPlayer : MonoBehaviour
    {
        public static LocalPlayer Instance;
        
        public bool IsVR
        {
            get
            {
                List<XRDisplaySubsystem> d = new List<XRDisplaySubsystem>();
                SubsystemManager.GetInstances(d);
                foreach (XRDisplaySubsystem xrDisplaySubsystem in d)
                    if (xrDisplaySubsystem.running)
                        return true;
                return false;
            }
        }

        private bool mic;
        public bool MicrophoneEnabled
        {
            get => mic;
            set
            {
                if (value)
                {
                    string device;
                    if (string.IsNullOrEmpty(ConfigManager.LoadedConfig.SelectedMicrophone))
                    {
                        device = Mic.Instance.Devices[0];
                        ConfigManager.LoadedConfig.SelectedMicrophone = device;
                        ConfigManager.SaveConfigToFile();
                    }
                    else if (Mic.Instance.Devices.Contains(ConfigManager.LoadedConfig.SelectedMicrophone))
                        device = ConfigManager.LoadedConfig.SelectedMicrophone;
                    else
                        return;
                    Mic.Instance.SetDeviceIndex(Mic.Instance.Devices.IndexOf(device));
                    Mic.Instance.StartRecording(48000, 5);
                }
                else
                {
                    Mic.Instance.StopRecording();
                }
                mic = value;
            }
        }

        public List<IBinding> Bindings = new()
        {
            new Keyboard()
                .RegisterCustomKeyDownEvent(KeyCode.V, () => Instance.MicrophoneEnabled = !Instance.MicrophoneEnabled),
            new Mouse()
        };

        public DashboardManager Dashboard;
        public CameraOffset CameraOffset;
        public TrackedPoseDriver TrackedPoseDriver;
        public CharacterController CharacterController;

        private PlayerUpdate GetPlayerUpdate(GameInstance gameInstance)
        {
            if (!APIPlayer.IsFullReady)
                return null;
            PlayerUpdate playerUpdate = new PlayerUpdate
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = gameInstance.userIdToken
                },
                AvatarId = ConfigManager.LoadedConfig.CurrentAvatarId,
                IsPlayerVR = IsVR
            };
            return playerUpdate;
        }
        
        private static byte[] ConvertAudioClip(IReadOnlyCollection<float> samples)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            int length = samples.Count;
            writer.Write(length);
            foreach (float sample in samples)
                writer.Write(sample);
            return stream.ToArray();
        }

        private PlayerVoice GetPlayerVoice(GameInstance gameInstance, byte[] inData, int l)
        {
            if (!APIPlayer.IsFullReady || !MicrophoneEnabled)
                return null;
            PlayerVoice playerVoice = new PlayerVoice
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = gameInstance.userIdToken
                },
                SampleRate = 48000,
                Channels = 1,
                FrameSize = 5,
                Encoder = "opus"
            };
            // If you don't want me to use obsolete, then update your README please!!
            using OpusEncoder opusEncoder = new OpusEncoder(Application.VoIP, 48000, 1)
            {
                Bitrate = 128000,
                VBR = true
            };
            byte[] data = opusEncoder.Encode(inData, l, out playerVoice.EncodeLength);
            playerVoice.Bytes = data;
            return playerVoice;
        }

        private void Start()
        {
            if (Instance != null)
            {
                Logger.CurrentLogger.Log("LocalPlayer already exists!");
                Destroy(this);
                return;
            }
            Instance = this;
            Bindings[1].ButtonClick += () => Dashboard.ToggleDashboard(this);
            Mic.Instance.OnSampleReady += (i, floats) =>
            {
                if (GameInstance.FocusedInstance == null)
                    return;
                byte[] d = ConvertAudioClip(floats);
                PlayerVoice playerVoice = GetPlayerVoice(GameInstance.FocusedInstance, d, i);
                if(playerVoice != null)
                    GameInstance.FocusedInstance.SendMessage(Msg.Serialize(playerVoice));
            };
        }

        private void Update()
        {
            bool vr = IsVR;
            CameraOffset.enabled = vr;
            TrackedPoseDriver.enabled = vr;
            foreach (IBinding binding in new List<IBinding>(Bindings))
                binding.Update();
            if (GameInstance.FocusedInstance == null)
                return;
            PlayerUpdate playerUpdate = GetPlayerUpdate(GameInstance.FocusedInstance);
            GameInstance.FocusedInstance.SendMessage(Msg.Serialize(playerUpdate), MessageChannel.Unreliable);
        }
    }
}