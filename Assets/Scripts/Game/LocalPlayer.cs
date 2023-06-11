using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Adrenak.UniMic;
using Hypernex.CCK.Unity;
using Hypernex.Configuration;
using Hypernex.ExtendedTracking;
using Hypernex.Game.Bindings;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using Nexport;
using OpusDotNet;
using UnityEditor;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using Application = OpusDotNet.Application;
using Avatar = Hypernex.CCK.Unity.Avatar;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;
using Keyboard = Hypernex.Game.Bindings.Keyboard;
using Logger = Hypernex.CCK.Logger;
using Mouse = Hypernex.Game.Bindings.Mouse;

namespace Hypernex.Game
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class LocalPlayer : MonoBehaviour
    {
        public static LocalPlayer Instance;
        public static readonly List<string> MorePlayerAssignedTags = new();
        public static readonly Dictionary<string, object> MoreExtraneousObjects = new();

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
                    SetMicrophone();
                    Mic.Instance.StartRecording(48000, 5);
                }
                else
                {
                    Mic.Instance.StopRecording();
                }
                mic = value;
            }
        }
        
        public DontDestroyMe DontDestroyMe { get; private set; }

        private Dictionary<InputDevice, GameObject> WorldTrackers = new();
        private List<InputDevice> trackers = new();
        
        public float WalkSpeed { get; set; } = 5f;
        public float RunSpeed { get; set; } = 10f;
        public float JumpHeight { get; set; } = 1.0f;
        public float Gravity { get; set; } = -9.87f;
        public bool LockMovement { get; set; }
        public bool LockCamera { get; set; }

        public List<IBinding> Bindings = new()
        {
            new Keyboard()
                .RegisterCustomKeyDownEvent(KeyCode.V, () => Instance.MicrophoneEnabled = !Instance.MicrophoneEnabled),
            new Mouse()
        };

        public DashboardManager Dashboard;
        public CameraOffset CameraOffset;
        public Camera Camera;
        public List<TrackedPoseDriver> TrackedPoseDriver;
        public CharacterController CharacterController;
        public Transform LeftHandReference;
        public Transform RightHandReference;
        public PlayerInput vrPlayerInput;
        public List<string> LastPlayerAssignedTags = new();
        public Dictionary<string, object> LastExtraneousObjects = new();

        private VRBindings vrBindings;
        private float verticalVelocity;
        private float groundedTimer;
        internal AvatarMeta avatarMeta;
        internal AvatarCreator avatar;
        private string avatarFile;
        private List<Transform> SavedTransforms = new();
        internal Dictionary<string, string> AvatarIdTokens = new();

        private void SetMicrophone()
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
        }

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
                AvatarId = ConfigManager.SelectedConfigUser.CurrentAvatar,
                IsPlayerVR = IsVR,
                TrackedObjects = new List<NetworkedObject>
                {
                    new()
                    {
                        ObjectLocation = String.Empty,
                        Position = NetworkConversionTools.Vector3Tofloat3(transform.position),
                        Rotation = NetworkConversionTools.QuaternionTofloat4(transform.rotation),
                        Size = NetworkConversionTools.Vector3Tofloat3(transform.localScale)
                    }
                },
                PlayerAssignedTags = new List<string>(),
                ExtraneousData = new Dictionary<string, object>(),
                WeightedObjects = new Dictionary<string, float>()
            };
            foreach (Transform child in new List<Transform>(SavedTransforms))
                playerUpdate.TrackedObjects.Add(new NetworkedObject
                {
                    ObjectLocation = AnimationUtility.CalculateTransformPath(child, transform),
                    Position = NetworkConversionTools.Vector3Tofloat3(child.position),
                    Rotation = NetworkConversionTools.QuaternionTofloat4(transform.rotation),
                    Size = NetworkConversionTools.Vector3Tofloat3(transform.localScale)
                });
            if(avatar != null)
                foreach (var animatorWeight in avatar.GetAnimatorWeights())
                    if(!playerUpdate.WeightedObjects.ContainsKey(animatorWeight.Key))
                        playerUpdate.WeightedObjects.Add(animatorWeight.Key, animatorWeight.Value);
            if (playerUpdate.IsPlayerVR)
            {
                XRBinding left = null;
                XRBinding right = null;
                foreach (IBinding binding in Bindings)
                    switch (binding.Id)
                    {
                        case "Left VRController":
                            left = (XRBinding) binding;
                            break;
                        case "Right VRController":
                            right = (XRBinding) binding;
                            break;
                    }

                if (left != null && right != null)
                {
                    List<(string, float)> fingerTrackingWeights = XRBinding.GetFingerTrackingWeights(left, right);
                    foreach ((string, float) fingerTrackingWeight in fingerTrackingWeights)
                        playerUpdate.WeightedObjects.Add(fingerTrackingWeight.Item1, fingerTrackingWeight.Item2);
                }
            }
            foreach (string s in new List<string>(MorePlayerAssignedTags))
                if(!playerUpdate.PlayerAssignedTags.Contains(s))
                    playerUpdate.PlayerAssignedTags.Add(s);
            foreach (KeyValuePair<string,object> extraneousObject in new Dictionary<string, object>(MoreExtraneousObjects))
                if(!playerUpdate.ExtraneousData.ContainsKey(extraneousObject.Key))
                    playerUpdate.ExtraneousData.Add(extraneousObject.Key, extraneousObject.Value);
            LastPlayerAssignedTags = new List<string>(playerUpdate.PlayerAssignedTags);
            LastExtraneousObjects = new Dictionary<string, object>(playerUpdate.ExtraneousData);
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

        private PlayerVoice GetPlayerVoice(GameInstance gameInstance, byte[] inData)
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
            byte[] data = opusEncoder.Encode(inData, 5, out playerVoice.EncodeLength);
            playerVoice.Bytes = data;
            return playerVoice;
        }

        private void OnAvatarDownload(string file, AvatarMeta am)
        {
            if (!File.Exists(file))
                return;
            AvatarCreator lastAvatar = avatar;
            Avatar a = AssetBundleTools.LoadAvatarFromFile(file);
            if (a == null)
            {
                avatar = lastAvatar;
                return;
            }
            avatarMeta = am;
            lastAvatar?.Dispose();
            avatar = new AvatarCreator(this, a, IsVR);
            avatarFile = file;
            SavedTransforms.Clear();
            foreach (Transform child in transform.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = 7;
                SavedTransforms.Add(child);
            }
        }

        private void GetTokenForOwnerAvatar(string avatarId, Action<string> result)
        {
            APIPlayer.APIObject.AddAssetToken(r =>
            {
                if (!r.success)
                {
                    GetTokenForOwnerAvatar(avatarId, result);
                    return;
                }
                QuickInvoke.InvokeActionOnMainThread(result, r.result.token.content);
            }, APIPlayer.APIUser, APIPlayer.CurrentToken, avatarId);
        }

        private void OnAvatarMeta(CallbackResult<MetaCallback<AvatarMeta>> r)
        {
            if (!r.success)
            {
                APIPlayer.APIObject.GetAvatarMeta(OnAvatarMeta, ConfigManager.SelectedConfigUser.CurrentAvatar);
                return;
            }
            Builds build = r.result.Meta.Builds.FirstOrDefault(x => x.BuildPlatform == AssetBundleTools.Platform);
            if (build == null)
                return;
            string file = $"{APIPlayer.APIObject.Settings.APIURL}file/{r.result.Meta.OwnerId}/{build.FileId}";
            if (r.result.Meta.Publicity != AvatarPublicity.Anyone)
            {
                if(r.result.Meta.OwnerId != APIPlayer.APIUser.Id)
                    return;
                // TODO: Share token in instance, excluding blocked users
                GetTokenForOwnerAvatar(r.result.Meta.Id, t =>
                {
                    AvatarIdTokens.Add(r.result.Meta.Id, t);
                    file = $"{APIPlayer.APIObject.Settings.APIURL}file/{r.result.Meta.OwnerId}/{build.FileId}/{t}";
                    DownloadTools.DownloadFile(file, $"{r.result.Meta.Id}.hna", f => OnAvatarDownload(f, r.result.Meta));
                });
                return;
            }
            DownloadTools.DownloadFile(file, $"{r.result.Meta.Id}.hna", f => OnAvatarDownload(f, r.result.Meta));
        }

        public void LoadAvatar()
        {
            if (string.IsNullOrEmpty(ConfigManager.SelectedConfigUser.CurrentAvatar))
                return;
            APIPlayer.APIObject.GetAvatarMeta(OnAvatarMeta, ConfigManager.SelectedConfigUser.CurrentAvatar);
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
            DontDestroyMe = gameObject.GetComponent<DontDestroyMe>();
            APIPlayer.OnUser += _ => LoadAvatar();
            CharacterController.minMoveDistance = 0;
            LockCamera = Dashboard.IsVisible;
            LockMovement = Dashboard.IsVisible;
            Bindings[1].Button2Click += () =>
            {
                Dashboard.ToggleDashboard(this);
                LockCamera = Dashboard.IsVisible;
                LockMovement = Dashboard.IsVisible;
            };
            Mic.Instance.OnSampleReady += (i, floats) =>
            {
                if (GameInstance.FocusedInstance == null)
                    return;
                byte[] d = ConvertAudioClip(floats);
                PlayerVoice playerVoice = GetPlayerVoice(GameInstance.FocusedInstance, d);
                if(playerVoice != null)
                    GameInstance.FocusedInstance.SendMessage(Msg.Serialize(playerVoice));
            };
            if (IsVR)
            {
                Bindings.Clear();
                // Create Bindings
                vrPlayerInput.ActivateInput();
                vrBindings = new VRBindings();
                XRBinding leftBinding = new XRBinding(vrBindings, true);
                XRBinding rightBinding = new XRBinding(vrBindings, false);
                Bindings.Add(leftBinding);
                Bindings.Add(rightBinding);
                Logger.CurrentLogger.Log("Added VR Bindings");
            }
        }

        private float rotx;
        private float s_;

        private (Vector3, bool, bool)? HandleBinding(IBinding binding)
        {
            if (!LockCamera && binding.Id == "Mouse")
            {
                transform.Rotate(0, (binding.Left * -1 + binding.Right) * ((Mouse)binding).Sensitivity, 0);
                rotx += -(binding.Up + binding.Down * -1) * ((Mouse) binding).Sensitivity;
                rotx = Mathf.Clamp(rotx, -90f, 90f);
                Camera.transform.localEulerAngles = new Vector3(rotx, 0, 0);
                return null;
            }
            if (!LockCamera && binding.IsLook)
            {
                // Right-Hand
                transform.Rotate(0, (binding.Left * -1 + binding.Right) * ((Mouse)binding).Sensitivity, 0);
                return null;
            }
            if (LockMovement)
                return null;
            // Left-Hand
            Vector3 move = transform.forward * (binding.Up + binding.Down * -1) + transform.right * (binding.Left * -1 + binding.Right);
            s_ = binding.Button2 ? RunSpeed : WalkSpeed;
            if (GameInstance.FocusedInstance != null)
                if(GameInstance.FocusedInstance.World != null)
                    if (!GameInstance.FocusedInstance.World.AllowRunning)
                        s_ = WalkSpeed;
            return (move * s_, binding.Button, binding.Up > 0.01f || binding.Down > 0.01f || binding.Left > 0.01f || binding.Right > 0.01f);
        }

        private bool areTwoTriggersClicked()
        {
            bool left = false;
            bool right = false;
            foreach (IBinding binding in Bindings)
            {
                if (binding.IsLook && binding.Trigger >= 0.8f)
                    left = true;
                if (!binding.IsLook && binding.Trigger >= 0.8f)
                    right = true;
            }
            return left && right;
        }

        // TODO: maybe we should cache an avatar instead? would improve speeds for HDD users, but increase memory usage
        public void RefreshAvatar() => OnAvatarDownload(avatarFile, avatarMeta);

        private void Update()
        {
            bool vr = IsVR;
            CameraOffset.enabled = vr;
            foreach (TrackedPoseDriver trackedPoseDriver in TrackedPoseDriver)
                trackedPoseDriver.enabled = vr;
            if (vr)
            {
                trackers.Clear();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.TrackingReference, trackers);
                foreach (InputDevice inputDevice in trackers)
                {
                    if (!WorldTrackers.ContainsKey(inputDevice))
                    {
                        GameObject gameObject = new GameObject(inputDevice.name);
                        gameObject.transform.SetParent(transform);
                        WorldTrackers.Add(inputDevice, gameObject);
                    }
                    else
                    {
                        bool a = inputDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p);
                        bool b = inputDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion r);
                        if (a && b)
                            WorldTrackers[inputDevice].transform.SetPositionAndRotation(p, r);
                    }
                }
            }
            Cursor.lockState = vr || LockCamera ? CursorLockMode.None : CursorLockMode.Locked;
            bool groundedPlayer = CharacterController.isGrounded;
            if (!LockMovement)
            {
                if (groundedPlayer)
                    groundedTimer = 0.2f;
                if (groundedTimer > 0)
                    groundedTimer -= Time.deltaTime;
                if (groundedPlayer && verticalVelocity < 0)
                    verticalVelocity = 0f;
                verticalVelocity += Gravity * Time.deltaTime;
            }
            (Vector3, bool, bool)? m = null;
            foreach (IBinding binding in new List<IBinding>(Bindings))
            {
                binding.Update();
                (Vector3, bool, bool)? r = HandleBinding(binding);
                if (r != null)
                    m = r.Value;
            }
            if (m != null && !LockMovement)
            {
                Vector3 move = m.Value.Item1;
                if (m.Value.Item2)
                    if (groundedTimer > 0)
                    {
                        groundedTimer = 0;
                        verticalVelocity += Mathf.Sqrt(JumpHeight * 2 * -Gravity);
                    }
                move.y = verticalVelocity;
                CharacterController.Move(move * Time.deltaTime);
            }
            if (GameInstance.FocusedInstance != null)
            {
                PlayerUpdate playerUpdate = GetPlayerUpdate(GameInstance.FocusedInstance);
                GameInstance.FocusedInstance.SendMessage(Msg.Serialize(playerUpdate), MessageChannel.Unreliable);
            }
            avatar?.SetSpeed(m?.Item3 ?? false ? s_ : 0.0f);
            avatar?.SetIsGrounded(groundedPlayer);
            avatar?.Update(areTwoTriggersClicked(), new Dictionary<InputDevice, GameObject>(WorldTrackers),
                Camera.transform, LeftHandReference, RightHandReference);
            if (ConfigManager.SelectedConfigUser != null && ConfigManager.SelectedConfigUser.UseFacialTracking &&
                FaceTrackingManager.HasInitialized)
            {
                // TODO: Universal Eyes
                Dictionary<FaceExpressions, float> faceWeights = FaceTrackingManager.GetFaceWeights();
                avatar?.UpdateFace(faceWeights);
                foreach (KeyValuePair<FaceExpressions,float> faceWeight in faceWeights)
                    avatar?.SetParameter(faceWeight.Key.ToString(), faceWeight.Value);
            }
            if(GameInstance.FocusedInstance != null && !GameInstance.FocusedInstance.authed)
                GameInstance.FocusedInstance.__SendMessage(Msg.Serialize(new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = GameInstance.FocusedInstance.userIdToken
                }));
        }

        private void LateUpdate()
        {
            avatar?.LateUpdate(IsVR, Camera.transform);
        }
    }
}