using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Adrenak.UniMic;
using Hypernex.CCK;
using Hypernex.Configuration;
using Hypernex.Game.Bindings;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.Tools;
using Hypernex.UI;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using Microsoft.MixedReality.Toolkit.Utilities;
using Nexport;
using OpusDotNet;
using RootMotion.FinalIK;
using UnityEditor;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using Application = OpusDotNet.Application;
using Avatar = Hypernex.CCK.Unity.Avatar;
using Logger = Hypernex.CCK.Logger;

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

        public Transform GetBoneFromHumanoid(HumanBodyBones humanBodyBones)
        {
            if (mainAnimator == null)
                return null;
            return mainAnimator.GetBoneTransform(humanBodyBones);
        }

        private VRBindings vrBindings;
        private float verticalVelocity;
        private float groundedTimer;
        internal AvatarMeta avatarMeta;
        internal Avatar avatar;
        private string lastAvatarFile;
        internal Animator mainAnimator;
        private VRIK vrik;
        private List<Sandbox> localAvatarSandboxes = new();
        private bool isCalibrating;
        private bool calibrated;
        private VRIKCalibrator.CalibrationData calibrationData = new();

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
                AvatarId = ConfigManager.LoadedConfig.CurrentAvatarId,
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
                }
            };
            // TODO: please cache!
            foreach (Transform child in transform.GetComponentsInChildren<Transform>())
                playerUpdate.TrackedObjects.Add(new NetworkedObject
                {
                    ObjectLocation = AnimationUtility.CalculateTransformPath(child, transform),
                    Position = NetworkConversionTools.Vector3Tofloat3(child.position),
                    Rotation = NetworkConversionTools.QuaternionTofloat4(transform.rotation),
                    Size = NetworkConversionTools.Vector3Tofloat3(transform.localScale)
                });
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

        private void OnAvatarDownload(string file)
        {
            if (!File.Exists(file))
            {
                avatarMeta = null;
                return;
            }
            Avatar lastAvatar = avatar;
            avatar = AssetBundleTools.LoadAvatarFromFile(file);
            if (avatar == null)
            {
                avatarMeta = null;
                avatar = lastAvatar;
                return;
            }
            foreach (Sandbox localAvatarSandbox in new List<Sandbox>(localAvatarSandboxes))
            {
                localAvatarSandboxes.Remove(localAvatarSandbox);
                localAvatarSandbox.Dispose();
            }
            Destroy(lastAvatar.gameObject);
            lastAvatarFile = file;
            DontDestroyOnLoad(avatar.gameObject);
            mainAnimator = avatar.GetComponent<Animator>();
            avatar.transform.SetParent(transform);
            avatar.transform.SetLocalPositionAndRotation(new Vector3(0, -1, 0), new Quaternion(0, 0, 0, 0));
            if(IsVR)
                vrik = avatar.gameObject.AddComponent<VRIK>();
            isCalibrating = false;
            calibrated = false;
            foreach (NexboxScript localAvatarScript in avatar.LocalAvatarScripts)
                localAvatarSandboxes.Add(new Sandbox(localAvatarScript, SandboxRestriction.LocalAvatar));
        }

        private void OnAvatarMeta(CallbackResult<MetaCallback<AvatarMeta>> r)
        {
            if (!r.success)
            {
                APIPlayer.APIObject.GetAvatarMeta(OnAvatarMeta, ConfigManager.LoadedConfig.CurrentAvatarId);
                return;
            }
            avatarMeta = r.result.Meta;
            string file;
            try
            {
                Builds build = avatarMeta.Builds.First(x => x.BuildPlatform == AssetBundleTools.Platform);
                file = $"{APIPlayer.APIObject.Settings.APIURL}{avatarMeta.OwnerId}/{build.FileId}";
            }
            catch (InvalidOperationException)
            {
                return;
            }
            DownloadTools.DownloadFile(file, $"{avatarMeta.Id}.hna", OnAvatarDownload);
        }

        private void LoadAvatar()
        {
            if (string.IsNullOrEmpty(ConfigManager.LoadedConfig.CurrentAvatarId))
                return;
            APIPlayer.APIObject.GetAvatarMeta(OnAvatarMeta, ConfigManager.LoadedConfig.CurrentAvatarId);
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
                PlayerVoice playerVoice = GetPlayerVoice(GameInstance.FocusedInstance, d, i);
                if(playerVoice != null)
                    GameInstance.FocusedInstance.SendMessage(Msg.Serialize(playerVoice));
            };
            if (IsVR)
            {
                // Create Bindings
                vrBindings = new VRBindings();
                MRTKBinding leftBinding = new MRTKBinding(Handedness.Left, vrBindings);
                MRTKBinding rightBinding = new MRTKBinding(Handedness.Right, vrBindings);
                Bindings.Add(leftBinding);
                Bindings.Add(rightBinding);
                Logger.CurrentLogger.Log("Added VR Bindings");
            }
            LoadAvatar();
        }

        private float rotx;

        private (Vector3, bool)? HandleBinding(IBinding binding)
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
            float speed = binding.Button2 ? RunSpeed : WalkSpeed;
            if (GameInstance.FocusedInstance != null)
                if(GameInstance.FocusedInstance.World != null)
                    if (!GameInstance.FocusedInstance.World.AllowRunning)
                        speed = WalkSpeed;
            return (move * speed, binding.Button);
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

        /// <summary>
        /// Sorts Trackers from 0 by how close they are to the Body, LeftFoot, and RightFoot
        /// </summary>
        /// <returns>Sorted Tracker Transforms</returns>
        private Transform[] FindClosestTrackers(Transform body, Transform leftFoot, Transform rightFoot, GameObject[] ts)
        {
            List<Transform> newTs = new();
            foreach (GameObject o in ts)
            {
                float bodyDist = Vector3.Distance(body.position, o.transform.position);
                float leftFootDist = Vector3.Distance(leftFoot.position, o.transform.position);
                float rightFootDist = Vector3.Distance(rightFoot.position, o.transform.position);
                Transform target;
                if (leftFootDist < bodyDist)
                    target = leftFoot;
                else if (rightFootDist < bodyDist)
                    target = rightFoot;
                else
                    target = body;
                if(!newTs.Contains(target))
                    newTs.Add(target);
            }
            return newTs.ToArray();
        }

        // TODO: maybe we should cache an avatar instead? would improve speeds for HDD users, but increase memory usage
        public void RefreshAvatar() => OnAvatarDownload(lastAvatarFile);

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
            (Vector3, bool)? m = null;
            foreach (IBinding binding in new List<IBinding>(Bindings))
            {
                binding.Update();
                (Vector3, bool)? r = HandleBinding(binding);
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
            if (!calibrated && WorldTrackers.Count == 3 && vrik != null)
                isCalibrating = true;
            else if (WorldTrackers.Count != 3 && vrik != null)
            {
                VRIKCalibrator.Calibrate(vrik, calibrationData, Camera.transform, null, LeftHandReference.transform,
                    RightHandReference.transform);
                isCalibrating = false;
                calibrated = true;
            }
            if (isCalibrating && areTwoTriggersClicked() && vrik != null)
            {
                GameObject[] ts = new GameObject[3];
                int i = 0;
                foreach (KeyValuePair<InputDevice, GameObject> keyValuePair in
                         new Dictionary<InputDevice, GameObject>(WorldTrackers))
                {
                    ts[i] = keyValuePair.Value;
                }
                if (ts[0] != null && ts[1] != null && ts[2] != null)
                {
                    Transform body = GetBoneFromHumanoid(HumanBodyBones.Hips);
                    Transform leftFoot = GetBoneFromHumanoid(HumanBodyBones.LeftFoot);
                    Transform rightFoot = GetBoneFromHumanoid(HumanBodyBones.RightFoot);
                    if (body != null && leftFoot != null && rightFoot != null)
                    {
                        Transform[] newTs = FindClosestTrackers(body, leftFoot, rightFoot, ts);
                        VRIKCalibrator.Calibrate(vrik, calibrationData, Camera.transform, newTs[0].transform,
                            LeftHandReference.transform, RightHandReference.transform, newTs[1], newTs[2]);
                        calibrated = true;
                        isCalibrating = false;
                    }
                }
            }
        }
    }
}