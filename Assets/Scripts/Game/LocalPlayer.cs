using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using Avatar = Hypernex.CCK.Unity.Avatar;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;
using Keyboard = Hypernex.Game.Bindings.Keyboard;
using Logger = Hypernex.CCK.Logger;
using Mic = Hypernex.Tools.Mic;
using Mouse = Hypernex.Game.Bindings.Mouse;

namespace Hypernex.Game
{
    [RequireComponent(typeof(DontDestroyMe))]
    [RequireComponent(typeof(OpusHandler))]
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
                    Mic.Instance.StartRecording();
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

        public List<IBinding> Bindings = new();

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
        public AvatarCreator avatar;
        private string avatarFile;
        private List<PathDescriptor> SavedTransforms = new();
        internal Dictionary<string, string> OwnedAvatarIdTokens = new();
        private OpusHandler opusHandler;

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
            Mic.Instance.SetDevice(device);
            opusHandler.OnMicStart();
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
                IsSpeaking = MicrophoneEnabled,
                PlayerAssignedTags = new List<string>(),
                ExtraneousData = new Dictionary<string, object>(),
                WeightedObjects = new Dictionary<string, float>()
            };
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

        private List<NetworkedObject> GetNetworkObjects()
        {
            Transform r = transform;
            Vector3 rea = transform.eulerAngles;
            List<NetworkedObject> networkedObjects = new()
            {
                new NetworkedObject
                {
                    ObjectLocation = String.Empty,
                    Position = NetworkConversionTools.Vector3Tofloat3(r.position),
                    Rotation = NetworkConversionTools.QuaternionTofloat4(new Quaternion(rea.x, rea.y, rea.z, 0)),
                    Size = NetworkConversionTools.Vector3Tofloat3(r.localScale)
                }
            };
            foreach (PathDescriptor child in new List<PathDescriptor>(SavedTransforms))
            {
                Transform t = child.transform;
                Vector3 lea = t.localEulerAngles;
                networkedObjects.Add(new NetworkedObject
                {
                    ObjectLocation = child.path,
                    Position = NetworkConversionTools.Vector3Tofloat3(t.localPosition),
                    Rotation = NetworkConversionTools.QuaternionTofloat4(new Quaternion(lea.x, lea.y, lea.z, 0)),
                    Size = NetworkConversionTools.Vector3Tofloat3(t.localScale)
                });
            }
            return networkedObjects;
        }

        private List<PlayerObjectUpdate> GetPlayerObjectUpdates()
        {
            List<PlayerObjectUpdate> p = new List<PlayerObjectUpdate>();
            foreach (NetworkedObject networkedObject in GetNetworkObjects())
            {
                p.Add(new PlayerObjectUpdate
                {
                    Auth = new JoinAuth
                    {
                        UserId = APIPlayer.APIUser.Id,
                        TempToken = GameInstance.FocusedInstance.userIdToken
                    },
                    Object = networkedObject
                });
            }
            return p;
        }

        private void OnOpusEncoded(byte[] pcmBytes, int pcmLength)
        {
            PlayerVoice playerVoice = new PlayerVoice
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = GameInstance.FocusedInstance.userIdToken
                },
                Bitrate = OpusHandler.BITRATE,
                SampleRate = (int) Mic.Frequency,
                Channels = (int) Mic.NumChannels,
                Encoder = "opus",
                Bytes = pcmBytes,
                EncodeLength = pcmLength
            };
            GameInstance.FocusedInstance.SendMessage(Msg.Serialize(playerVoice));
        }

        private void ShareAvatarTokenToConnectedUsersInInstance(AvatarMeta am)
        {
            // Share AvatarToken with unblocked users
            if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.IsOpen && APIPlayer.IsFullReady)
            {
                foreach (User connectedUser in GameInstance.FocusedInstance.ConnectedUsers)
                {
                    if (!APIPlayer.APIUser.BlockedUsers.Contains(connectedUser.Id))
                    {
                        APIPlayer.APIObject.AddAssetToken(rr =>
                        {
                            APIPlayer.UserSocket.ShareAvatarToken(connectedUser, am.Id, rr.result.token.content);
                        }, APIPlayer.APIUser, APIPlayer.CurrentToken, am.Id);
                    }
                }
            }
        }

        private void ShareAvatarTokenToUserId(User connectedUser, AvatarMeta am)
        {
            if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.IsOpen && APIPlayer.IsFullReady)
            {
                if (!APIPlayer.APIUser.BlockedUsers.Contains(connectedUser.Id))
                {
                    APIPlayer.APIObject.AddAssetToken(rr =>
                    {
                        APIPlayer.UserSocket.ShareAvatarToken(connectedUser, am.Id, rr.result.token.content);
                    }, APIPlayer.APIUser, APIPlayer.CurrentToken, am.Id);
                }
            }
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
                PathDescriptor pathDescriptor = child.gameObject.GetComponent<PathDescriptor>();
                if (pathDescriptor == null)
                    pathDescriptor = child.gameObject.AddComponent<PathDescriptor>();
                pathDescriptor.root = transform;
                SavedTransforms.Add(pathDescriptor);
            }
            if (am.Publicity == AvatarPublicity.OwnerOnly)
                ShareAvatarTokenToConnectedUsersInInstance(am);
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
                    OwnedAvatarIdTokens.Add(r.result.Meta.Id, t);
                    file = $"{APIPlayer.APIObject.Settings.APIURL}file/{r.result.Meta.OwnerId}/{build.FileId}/{t}";
                    APIPlayer.APIObject.GetFileMeta(fileMetaResult =>
                    {
                        string knownHash = String.Empty;
                        if (fileMetaResult.success)
                            knownHash = fileMetaResult.result.FileMeta.Hash;
                        DownloadTools.DownloadFile(file, $"{r.result.Meta.Id}.hna",
                            f => OnAvatarDownload(f, r.result.Meta), knownHash);
                    }, r.result.Meta.OwnerId, build.FileId);
                });
                return;
            }
            APIPlayer.APIObject.GetFileMeta(fileMetaResult =>
            {
                string knownHash = String.Empty;
                if (fileMetaResult.success)
                    knownHash = fileMetaResult.result.FileMeta.Hash;
                DownloadTools.DownloadFile(file, $"{r.result.Meta.Id}.hna",
                    f => OnAvatarDownload(f, r.result.Meta), knownHash);
            }, r.result.Meta.OwnerId, build.FileId);
        }

        public void LoadAvatar()
        {
            if (string.IsNullOrEmpty(ConfigManager.SelectedConfigUser.CurrentAvatar))
                return;
            APIPlayer.APIObject.GetAvatarMeta(OnAvatarMeta, ConfigManager.SelectedConfigUser.CurrentAvatar);
        }

        private Coroutine lastCoroutine;
        private Queue<PlayerObjectUpdate> msgs = new();
        private CancellationTokenSource cts;
        private Mutex mutex = new();

        private IEnumerator UpdatePlayer(GameInstance gameInstance)
        {
            while (true)
            {
                if (gameInstance.IsOpen)
                {
                    PlayerUpdate playerUpdate = GetPlayerUpdate(gameInstance);
                    gameInstance.SendMessage(Msg.Serialize(playerUpdate), MessageChannel.Unreliable);
                    // TODO: Message Queues are slow, fix underlying issue in Nexport
                    if (mutex.WaitOne(1))
                    {
                        foreach (PlayerObjectUpdate playerObjectUpdate in GetPlayerObjectUpdates())
                        {
                            msgs.Enqueue(playerObjectUpdate);
                            //byte[] g = Msg.Serialize(playerObjectUpdate);
                            //gameInstance.SendMessage(g, MessageChannel.Unreliable);
                        }
                        mutex.ReleaseMutex();
                    }
                }
                yield return new WaitForSeconds(0.05f);
            }
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
            opusHandler = GetComponent<OpusHandler>();
            APIPlayer.OnUser += _ => LoadAvatar();
            CharacterController.minMoveDistance = 0;
            LockCamera = Dashboard.IsVisible;
            LockMovement = Dashboard.IsVisible;
            Bindings.Add(new Keyboard()
                .RegisterCustomKeyDownEvent(KeyCode.V, () => Instance.MicrophoneEnabled = !Instance.MicrophoneEnabled));
            Bindings.Add(new Mouse());
            Bindings[1].Button2Click += () =>
            {
                Dashboard.ToggleDashboard(this);
                LockCamera = Dashboard.IsVisible;
                LockMovement = Dashboard.IsVisible;
            };
            Mic.OnClipReady += samples =>
            {
                if (GameInstance.FocusedInstance == null || opusHandler == null)
                    return;
                opusHandler.EncodeMicrophone(samples);
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
            GameInstance.OnGameInstanceLoaded += (instance, meta) =>
            {
                lastCoroutine = StartCoroutine(UpdatePlayer(instance));
                instance.OnUserLoaded += user =>
                {
                    if (avatarMeta.Publicity == AvatarPublicity.OwnerOnly)
                        ShareAvatarTokenToUserId(user, avatarMeta);
                };
                instance.OnClientConnect += user =>
                {
                    if (avatarMeta.Publicity == AvatarPublicity.OwnerOnly)
                        ShareAvatarTokenToUserId(user, avatarMeta);
                };
                instance.OnDisconnect += () =>
                {
                    if (lastCoroutine != null)
                    {
                        StopCoroutine(lastCoroutine);
                        lastCoroutine = null;
                    }
                };
                if (avatarMeta.Publicity == AvatarPublicity.OwnerOnly)
                    ShareAvatarTokenToConnectedUsersInInstance(avatarMeta);
            };
            opusHandler.OnEncoded += OnOpusEncoded;
            cts = new();
            new Thread(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (mutex.WaitOne())
                    {
                        if (msgs.Count > 0)
                        {
                            for (int i = 0; i < msgs.Count; i++)
                            {
                                PlayerObjectUpdate p = msgs.Dequeue();
                                byte[] msg = Msg.Serialize(p);
                                GameInstance.FocusedInstance.SendMessage(msg, MessageChannel.Unreliable);
                            }
                        }
                        mutex.ReleaseMutex();
                    }
                    Thread.Sleep(10);
                }
            }).Start();
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

        private void OnDestroy()
        {
            foreach (IBinding binding in Bindings)
            {
                if(binding.GetType() == typeof(Keyboard))
                    ((Keyboard)binding).Dispose();
                if(binding.GetType() == typeof(Mouse))
                    ((Mouse)binding).Dispose();
            }
            if (lastCoroutine != null)
            {
                StopCoroutine(lastCoroutine);
                lastCoroutine = null;
            }
            opusHandler.OnEncoded -= OnOpusEncoded;
            cts?.Cancel();
        }
    }
}