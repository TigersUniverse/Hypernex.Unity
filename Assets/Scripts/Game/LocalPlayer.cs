using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Hypernex.CCK;
using Hypernex.CCK.Unity;
using Hypernex.Configuration;
using Hypernex.ExtendedTracking;
using Hypernex.Game.Bindings;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.Tools;
using Hypernex.UI;
using Hypernex.UI.Templates;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using Nexport;
using RNNoise.NET;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Keyboard = Hypernex.Game.Bindings.Keyboard;
using Logger = Hypernex.CCK.Logger;
using Mic = Hypernex.Tools.Mic;
using Mouse = Hypernex.Game.Bindings.Mouse;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace Hypernex.Game
{
    [RequireComponent(typeof(DontDestroyMe))]
    [RequireComponent(typeof(OpusHandler))]
    public class LocalPlayer : MonoBehaviour, IDisposable
    {
        public static LocalPlayer Instance;
        public static readonly List<string> MorePlayerAssignedTags = new();
        public static readonly Dictionary<string, object> MoreExtraneousObjects = new();

        public static bool IsVR { get; internal set; }

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

        //private Dictionary<InputDevice, GameObject> WorldTrackers = new();
        //private List<InputDevice> trackers = new();
        
        public float WalkSpeed { get; set; } = 5f;
        public float RunSpeed { get; set; } = 10f;
        public float JumpHeight { get; set; } = 1.0f;
        public float Gravity { get; set; } = -9.87f;
        public bool LockMovement { get; set; }
        public bool LockCamera { get; set; }

        public List<IBinding> Bindings = new();

        public DashboardManager Dashboard;
        public XROrigin XROrigin;
        public Camera Camera;
        public Camera UICamera;
        public Transform FakeVRHead;
        public List<TrackedPoseDriver> TrackedPoseDriver;
        public CharacterController CharacterController;
        public Transform LeftHandReference;
        public Transform LeftHandVRIKTarget;
        public Transform RightHandReference;
        public Transform RightHandVRIKTarget;
        public PlayerInput vrPlayerInput;
        public HandGetter LeftHandGetter;
        public HandGetter RightHandGetter;
        public List<string> LastPlayerAssignedTags = new();
        public List<XRInteractorLineVisual> XRRays = new ();
        public Dictionary<string, object> LastExtraneousObjects = new();
        public VRInputListener VRInputListener;
        public Vector3 LowestPoint;
        public float LowestPointRespawnThreshold = 50f;
        public CurrentAvatar CurrentAvatarDisplay;
        
        private const float MESSAGE_UPDATE_TIME = 0.05f;

        private Denoiser denoiser;
        private float verticalVelocity;
        private float groundedTimer;
        internal AvatarMeta avatarMeta;
        public AvatarCreator avatar;
        private string avatarFile;
        internal List<PathDescriptor> SavedTransforms = new();
        internal Dictionary<string, string> OwnedAvatarIdTokens = new();
        private OpusHandler opusHandler;
        private bool didSnapTurn;
        private Scene? scene;
        internal float vrHeight;

        public IEnumerator SafeSwitchScene(string s, Action<Scene> onAsyncDone = null, Action<Scene> onDone = null)
        {
            DontDestroyMe.Register();
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(s);
            yield return new WaitUntil(() => asyncOperation.isDone);
            Scene currentScene = SceneManager.GetSceneByPath(s);
            scene = currentScene;
            if(onAsyncDone != null)
                onAsyncDone.Invoke(currentScene);
            yield return new WaitUntil(() => currentScene.isLoaded);
            DontDestroyMe.MoveToScene(currentScene);
            if(onDone != null)
                onDone.Invoke(currentScene);
            LowestPoint = AnimationUtility.GetLowestObject(currentScene).position;
        }
        
        public IEnumerator SafeSwitchScene(int i, Action<Scene> onAsyncDone = null, Action<Scene> onDone = null)
        {
            DontDestroyMe.Register();
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(i);
            yield return new WaitUntil(() => asyncOperation.isDone);
            Scene currentScene = SceneManager.GetSceneByBuildIndex(i);
            scene = currentScene;
            if(onAsyncDone != null)
                onAsyncDone.Invoke(currentScene);
            yield return new WaitUntil(() => currentScene.isLoaded);
            DontDestroyMe.MoveToScene(currentScene);
            if(onDone != null)
                onDone.Invoke(currentScene);
            LowestPoint = AnimationUtility.GetLowestObject(currentScene).position;
        }

        // maybe we should cache an avatar instead? would improve speeds for HDD users, but increase memory usage
        public void RefreshAvatar(bool fromDash = false)
        {
            if(!fromDash)
                CurrentAvatarDisplay.RefreshAvatar(false);
            transform.localScale = new Vector3(1, 1, 1);
            Dashboard.PositionDashboard(this);
            OnAvatarDownload(avatarFile, avatarMeta);
        }

        public void Respawn(Scene? s = null)
        {
            Vector3 spawnPosition = new Vector3(0, 1, 0);
            if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World.SpawnPoints.Count > 0)
            {
                Transform spT = GameInstance.FocusedInstance.World
                    .SpawnPoints[new System.Random().Next(0, GameInstance.FocusedInstance.World.SpawnPoints.Count - 1)]
                    .transform;
                spawnPosition = spT.position;
            }
            else
            {
                GameObject searchSpawn;
                if (s == null)
                    searchSpawn = SceneManager.GetActiveScene().GetRootGameObjects()
                        .FirstOrDefault(x => x.name.ToLower() == "Spawn");
                else
                    searchSpawn = s.Value.GetRootGameObjects().FirstOrDefault(x => x.name.ToLower() == "Spawn");
                if (searchSpawn != null)
                    spawnPosition = searchSpawn.transform.position;
            }
            transform.position = spawnPosition;
        }

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

        private void AddSystemTags(ref List<string> tags)
        {
            if (!tags.Contains("*eyetracking") && FaceTrackingManager.EyeTracking)
                tags.Add("*eyetracking");
            if (!tags.Contains("*liptracking") && FaceTrackingManager.LipTracking)
                tags.Add("*liptracking");
        }

        private void TagsCheck(ref List<string> tags)
        {
            if (tags.Contains("*eyetracking") && !FaceTrackingManager.EyeTracking)
                tags.Remove("*eyetracking");
            if (tags.Contains("*liptracking") && !FaceTrackingManager.LipTracking)
                tags.Remove("*liptracking");
        }

        private PlayerUpdate GetPlayerUpdate(GameInstance gameInstance)
        {
            if (GameInstance.FocusedInstance == null || !GameInstance.FocusedInstance.IsOpen)
                return null;
            PlayerUpdate playerUpdate = new PlayerUpdate
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = gameInstance.userIdToken
                },
                IsPlayerVR = IsVR,
                IsSpeaking = MicrophoneEnabled,
                PlayerAssignedTags = new List<string>(),
                ExtraneousData = new Dictionary<string, object>(),
                WeightedObjects = new Dictionary<string, float>()
            };
            AddSystemTags(ref playerUpdate.PlayerAssignedTags);
            if (avatarMeta != null)
                playerUpdate.AvatarId = avatarMeta.Id;
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
            TagsCheck(ref playerUpdate.PlayerAssignedTags);
            LastPlayerAssignedTags = new List<string>(playerUpdate.PlayerAssignedTags);
            LastExtraneousObjects = new Dictionary<string, object>(playerUpdate.ExtraneousData);
            return playerUpdate;
        }

        internal Dictionary<string, NetworkedObject> children = new();
        private List<WeightedObjectUpdate> weightedObjectUpdates = new();

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
                },
                new NetworkedObject
                {
                    ObjectLocation = "Camera Offset",
                    Position = NetworkConversionTools.Vector3Tofloat3(Camera.transform.parent.localPosition),
                    Rotation = NetworkConversionTools.QuaternionTofloat4(new Quaternion(
                        Camera.transform.parent.localEulerAngles.x, Camera.transform.parent.localEulerAngles.y,
                        Camera.transform.parent.localEulerAngles.z, 0)),
                    Size = NetworkConversionTools.Vector3Tofloat3(Camera.transform.parent.localScale)
                },
                new NetworkedObject
                {
                    ObjectLocation = "Camera Offset/Main Camera",
                    Position = NetworkConversionTools.Vector3Tofloat3(Camera.transform.localPosition),
                    Rotation = NetworkConversionTools.QuaternionTofloat4(new Quaternion(
                        Camera.transform.localEulerAngles.x, Camera.transform.localEulerAngles.y,
                        Camera.transform.localEulerAngles.z, 0)),
                    Size = NetworkConversionTools.Vector3Tofloat3(Camera.transform.localScale)
                }
            };
            foreach (HandleCamera handleCamera in HandleCamera.allCameras)
            {
                networkedObjects.Add(new NetworkedObject
                {
                    ObjectLocation = "*" + handleCamera.gameObject.name,
                    IgnoreObjectLocation = true,
                    Position = NetworkConversionTools.Vector3Tofloat3(handleCamera.transform.position),
                    Rotation = NetworkConversionTools.QuaternionTofloat4(new Quaternion(
                        handleCamera.transform.eulerAngles.x,
                        handleCamera.transform.eulerAngles.y,
                        handleCamera.transform.eulerAngles.z, 0)),
                    Size = NetworkConversionTools.Vector3Tofloat3(handleCamera.transform.localScale)
                });
            }
            /*foreach (PathDescriptor child in new List<PathDescriptor>(pathsWaiting.Dequeue()))
            {
                if (child == null)
                    SavedTransforms.Remove(child);
                else
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
            }*/
            foreach (NetworkedObject networkedObject in new List<NetworkedObject>(children.Values))
                networkedObjects.Add(networkedObject);
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
            GameInstance.FocusedInstance.SendMessage(Msg.Serialize(playerVoice), MessageChannel.Unreliable);
        }

        private void OnAudioRAW(float[] data, int length)
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
                Encoder = "raw",
                Bytes = DataConversion.ConvertFloatToByte(data),
                EncodeLength = length
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
            StartCoroutine(AssetBundleTools.LoadAvatarFromFile(file, a =>
            {
                if (a == null)
                {
                    avatar = lastAvatar;
                    return;
                }
                avatarMeta = am;
                lastAvatar?.Dispose();
                CurrentAvatarDisplay.SizeAvatar(1f);
                avatar = new AvatarCreator(this, a, IsVR);
                foreach (NexboxScript localAvatarScript in a.LocalAvatarScripts)
                    avatar.localAvatarSandboxes.Add(new Sandbox(localAvatarScript, transform));
                avatarFile = file;
                // Why this doesn't clear old transforms? I don't know.
                SavedTransforms.Clear();
                children.Clear();
                foreach (Transform child in avatar.Avatar.transform.GetComponentsInChildren<Transform>())
                {
                    PathDescriptor pathDescriptor = child.gameObject.GetComponent<PathDescriptor>();
                    if (pathDescriptor == null)
                        pathDescriptor = child.gameObject.AddComponent<PathDescriptor>();
                    pathDescriptor.root = transform;
                    SavedTransforms.Add(pathDescriptor);
                }
                if (am.Publicity == AvatarPublicity.OwnerOnly)
                    ShareAvatarTokenToConnectedUsersInInstance(am);
                Dashboard.PositionDashboard(this);
            }));
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
                GetTokenForOwnerAvatar(r.result.Meta.Id, t =>
                {
                    if (OwnedAvatarIdTokens.ContainsKey(r.result.Meta.Id))
                        OwnedAvatarIdTokens.Remove(r.result.Meta.Id);
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
        private Queue<PlayerObjectUpdate> poumsgs = new();
        private Queue<WeightedObjectUpdate> woumsgs = new();
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
                    if (poumsgs.Count <= 0 && mutex.WaitOne(1))
                    {
                        try
                        {
                            foreach (PlayerObjectUpdate playerObjectUpdate in GetPlayerObjectUpdates())
                            {
                                poumsgs.Enqueue(playerObjectUpdate);
                                //byte[] g = Msg.Serialize(playerObjectUpdate);
                                //gameInstance.SendMessage(g, MessageChannel.Unreliable);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.CurrentLogger.Error("Failed to get PlayerUpdate! Exception: " + e);
                        }
                        mutex.ReleaseMutex();
                    }
                    if (woumsgs.Count <= 0 && mutex.WaitOne(1))
                    {
                        try
                        {
                            foreach (WeightedObjectUpdate weightedObjectUpdate in weightedObjectUpdates)
                            {
                                woumsgs.Enqueue(weightedObjectUpdate);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.CurrentLogger.Error("Failed to get PlayerUpdate! Exception: " + e);
                        }
                        mutex.ReleaseMutex();
                    }
                }
                yield return new WaitForSeconds(MESSAGE_UPDATE_TIME);
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
            CreateDesktopBindings();
            Mic.OnClipReady += s =>
            {
                float[] samples = s;
                if (ConfigManager.SelectedConfigUser != null && ConfigManager.SelectedConfigUser.NoiseSuppression)
                {
                    Span<float> values = new Span<float>(samples);
                    if (denoiser == null)
                        denoiser = new Denoiser();
                    denoiser.Denoise(values, false);
                    samples = values.ToArray();
                }
                if (GameInstance.FocusedInstance != null && ConfigManager.SelectedConfigUser != null &&
                    ConfigManager.SelectedConfigUser.AudioCompression == AudioCompression.Opus && opusHandler != null)
                    opusHandler.EncodeMicrophone(samples);
                else if(ConfigManager.SelectedConfigUser != null && ConfigManager.SelectedConfigUser.AudioCompression == AudioCompression.RAW)
                    OnAudioRAW(samples, samples.Length);
                avatar?.ApplyAudioClipToLipSync(samples);
            };
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
                    if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.IsOpen)
                    {
                        if (mutex.WaitOne())
                        {
                            if (poumsgs.Count > 0)
                            {
                                for (int i = 0; i < poumsgs.Count; i++)
                                {
                                    PlayerObjectUpdate p = poumsgs.Dequeue();
                                    byte[] msg = Msg.Serialize(p);
                                    GameInstance.FocusedInstance.SendMessage(msg, MessageChannel.Unreliable);
                                }
                            }
                            mutex.ReleaseMutex();
                        }
                    }
                    Thread.Sleep(10);
                }
            }).Start();
            new Thread(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.IsOpen)
                    {
                        if (mutex.WaitOne())
                        {
                            if (woumsgs.Count > 0)
                            {
                                for (int i = 0; i < woumsgs.Count; i++)
                                {
                                    WeightedObjectUpdate w = woumsgs.Dequeue();
                                    byte[] msg = Msg.Serialize(w);
                                    GameInstance.FocusedInstance.SendMessage(msg, MessageChannel.Unreliable);
                                }
                            }
                            mutex.ReleaseMutex();
                        }
                    }
                    Thread.Sleep(10);
                }
            }).Start();
        }

        private void SwitchVR()
        {
            if(IsVR)
            {
                Init.Instance.StopVR();
                CharacterController.center = Vector3.zero;
                CharacterController.height = 1.36144f;
                foreach (Camera allCamera in Camera.allCameras)
                    allCamera.fieldOfView = 60f;
            }
            else
                Init.Instance.StartVR();
            if(avatar != null)
                RefreshAvatar();
        }

        private void CreateDesktopBindings()
        {
            Bindings.Add(new Keyboard()
                .RegisterCustomKeyDownEvent(KeyCode.V, () => Instance.MicrophoneEnabled = !Instance.MicrophoneEnabled));
            Bindings.Add(new Mouse());
            Bindings[1].Button2Click += () => Dashboard.ToggleDashboard(this);
        }

        internal void StartVR()
        {
            AlignVR(true, true);
            Bindings.ForEach(x =>
            {
                if(x.GetType() == typeof(Keyboard))
                    ((Keyboard) x).Dispose();
                else if(x.GetType() == typeof(Mouse))
                    ((Mouse) x).Dispose();
            });
            Bindings.Clear();
            // Create Bindings
            vrPlayerInput.ActivateInput();
            XRBinding leftBinding = new XRBinding(false, LeftHandGetter);
            XRBinding rightBinding = new XRBinding(true, RightHandGetter);
            leftBinding.Button2Click += () => Dashboard.ToggleDashboard(this);
            rightBinding.Button2Click += () => Instance.MicrophoneEnabled = !Instance.MicrophoneEnabled;
            Bindings.Add(leftBinding);
            VRInputListener.AddXRBinding(leftBinding);
            Bindings.Add(rightBinding);
            VRInputListener.AddXRBinding(rightBinding);
            Logger.CurrentLogger.Log("Added VR Bindings");
            CoroutineRunner.Instance.Run(PositionDashboardOnVRSwitch());
        }

        private IEnumerator PositionDashboardOnVRSwitch()
        {
            yield return new WaitForSeconds(1f);
            if(Dashboard.IsVisible)
                Dashboard.PositionDashboard(this);
        }

        internal void StopVR()
        {
            foreach (IBinding binding in new List<IBinding>(Bindings))
            {
                if (binding.GetType() == typeof(XRBinding))
                    Bindings.Remove(binding);
            }
            vrPlayerInput.DeactivateInput();
            VRInputListener.Clear();
            CreateDesktopBindings();
            Logger.CurrentLogger.Log("Removed VR Bindings");
        }

        public void AlignVR(bool useConfig, bool o = false)
        {
            if (!IsVR && !o)
                return;
            // Align the character controller
            if (useConfig && ConfigManager.SelectedConfigUser != null)
            {
                if (ConfigManager.SelectedConfigUser.VRPlayerHeight == 0f)
                    return;
                vrHeight = ConfigManager.SelectedConfigUser.VRPlayerHeight;
            }
            else
            {
                vrHeight = Mathf.Clamp(XROrigin.CameraInOriginSpaceHeight, 0, Single.PositiveInfinity);
                if(ConfigManager.SelectedConfigUser != null)
                    ConfigManager.SelectedConfigUser.VRPlayerHeight = vrHeight;
            }
            Vector3 center = XROrigin.CameraInOriginSpacePos;
            center.y = vrHeight / 2f + CharacterController.skinWidth;
            CharacterController.height = vrHeight;
            CharacterController.center = center;
            if(avatar != null)
                RefreshAvatar();
        }

        private float rotx;
        private float s_;

        private (Vector3, bool, bool)? HandleLeftBinding(IBinding binding, bool vr)
        {
            // Left-Hand
            Vector3 move;
            if (vr)
                move = Camera.transform.forward * (binding.Up + binding.Down * -1) +
                       Camera.transform.right * (binding.Left * -1 + binding.Right);
            else
                move = transform.forward * (binding.Up + binding.Down * -1) +
                       transform.right * (binding.Left * -1 + binding.Right);
            s_ = binding.Button2 ? RunSpeed : WalkSpeed;
            if (GameInstance.FocusedInstance != null)
                if(GameInstance.FocusedInstance.World != null)
                    if (!GameInstance.FocusedInstance.World.AllowRunning)
                        s_ = WalkSpeed;
            return (move * s_, binding.Button,
                binding.Up > 0.01f || binding.Down > 0.01f || binding.Left > 0.01f || binding.Right > 0.01f);
        }

        private (Vector3, bool, bool)? HandleRightBinding(IBinding binding)
        {
            if (!LockCamera && binding.Id == "Mouse" && !IsVR)
            {
                transform.Rotate(0, (binding.Left * -1 + binding.Right) * ((Mouse)binding).Sensitivity, 0);
                rotx += -(binding.Up + binding.Down * -1) * ((Mouse) binding).Sensitivity;
                rotx = Mathf.Clamp(rotx, -90f, 90f);
                Camera.transform.localEulerAngles = new Vector3(rotx, 0, 0);
                return (Vector3.zero, binding.Button, false);
            }
            if (!LockCamera)
            {
                // Right-Hand
                if (ConfigManager.SelectedConfigUser != null && ConfigManager.SelectedConfigUser.UseSnapTurn)
                {
                    float amountTurn = binding.Left * -1 + binding.Right;
                    if (!didSnapTurn && (amountTurn > 0.1f || amountTurn < -0.1f))
                    {
                        float val = 1f;
                        if (amountTurn < 0)
                            val = -1f;
                        float turnDegree = 45f;
                        if (ConfigManager.SelectedConfigUser != null)
                            turnDegree = ConfigManager.SelectedConfigUser.SnapTurnAngle;
                        transform.Rotate(0, turnDegree * val, 0);
                        didSnapTurn = true;
                    }
                    else if (didSnapTurn && (amountTurn < 0.1f && amountTurn > -0.1f))
                        didSnapTurn = false;
                }
                else
                {
                    float turnSpeed = 1;
                    if (ConfigManager.SelectedConfigUser != null)
                        turnSpeed = ConfigManager.SelectedConfigUser.SmoothTurnSpeed;
                    transform.Rotate(0, (binding.Left * -1 + binding.Right) * turnSpeed, 0);
                }
                if (LockMovement)
                    return null;
                return (Vector3.zero, binding.Button, false);
            }
            if (LockMovement)
                return null;
            return (Vector3.zero, binding.Button, false);
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

        private void Update()
        {
            bool vr = IsVR;
            XROrigin.enabled = vr;
            foreach (TrackedPoseDriver trackedPoseDriver in TrackedPoseDriver)
                trackedPoseDriver.enabled = vr;
            LeftHandReference.GetChild(1).GetChild(0).gameObject.SetActive(vr && avatar == null);
            RightHandReference.GetChild(1).GetChild(0).gameObject.SetActive(vr && avatar == null);
            foreach (XRInteractorLineVisual lineVisual in XRRays)
                lineVisual.enabled = vr;
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
            (Vector3, bool, bool)? left_m = null;
            (Vector3, bool, bool)? right_m = null;
            foreach (IBinding binding in new List<IBinding>(Bindings))
            {
                binding.Update();
                bool g = !binding.IsLook;
                if (vr)
                    g = binding.IsLook;
                if (g)
                {
                    (Vector3, bool, bool)? r = HandleLeftBinding(binding, vr);
                    if (r != null)
                        left_m = r.Value;
                }
                else
                {
                    (Vector3, bool, bool)? r = HandleRightBinding(binding);
                    if (r != null)
                        right_m = r.Value;
                }
            }
            Vector3 move = new Vector3();
            if (right_m != null && right_m.Value.Item2)
                if (groundedTimer > 0)
                {
                    groundedTimer = 0;
                    verticalVelocity += Mathf.Sqrt(JumpHeight * 2 * -Gravity);
                }
            if (left_m != null && !LockMovement)
            {
                if(left_m.Value.Item3)
                {
                    move = left_m.Value.Item1;
                }
            }
            if (!LockMovement)
            {
                move = new Vector3(move.x, verticalVelocity, move.z);
                CharacterController.Move(move * Time.deltaTime);
                avatar?.SetSpeed(left_m?.Item3 ?? false ? s_ : 0.0f);
                avatar?.SetIsGrounded(groundedPlayer);
            }
            else
            {
                avatar?.SetSpeed(0.0f);
                avatar?.SetIsGrounded(true);
            }
            bool isMoving = left_m?.Item3 ?? false;
            avatar?.Update(areTwoTriggersClicked(), FakeVRHead, LeftHandVRIKTarget, RightHandVRIKTarget,
                isMoving);
            // TODO: Non-Eye Tracking Eye Movement
            if(GameInstance.FocusedInstance != null && !GameInstance.FocusedInstance.authed)
                GameInstance.FocusedInstance.__SendMessage(Msg.Serialize(new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = GameInstance.FocusedInstance.userIdToken
                }));
            if(transform.position.y < LowestPoint.y - Mathf.Abs(LowestPointRespawnThreshold))
                Respawn(scene);
            if (Input.GetKeyDown(KeyCode.F5))
                SwitchVR();
            if (GameInstance.FocusedInstance != null && ConfigManager.SelectedConfigUser != null)
            {
                foreach (KeyValuePair<AudioSource, float> keyValuePair in new Dictionary<AudioSource, float>(
                             GameInstance.FocusedInstance.worldAudios))
                {
                    keyValuePair.Key.volume =
                        ConfigManager.SelectedConfigUser.WorldAudioVolume * keyValuePair.Value;
                }
            }
        }

        private void LateUpdate()
        {
            avatar?.LateUpdate(IsVR, Camera.transform, LockCamera);
            if (ConfigManager.SelectedConfigUser != null && ConfigManager.SelectedConfigUser.UseFacialTracking &&
                FaceTrackingManager.HasInitialized)
            {
                avatar?.UpdateEyes(FaceTrackingManager.GetEyeWeights());
                Dictionary<FaceExpressions, float> faceWeights = FaceTrackingManager.GetFaceWeights();
                avatar?.UpdateFace(faceWeights);
                foreach (KeyValuePair<FaceExpressions,float> faceWeight in faceWeights)
                    avatar?.SetParameter(faceWeight.Key.ToString(), faceWeight.Value);
            }
            if(APIPlayer.APIUser != null && GameInstance.FocusedInstance != null)
                weightedObjectUpdates = avatar?.GetAnimatorWeights(new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = GameInstance.FocusedInstance.userIdToken
                });
        }

        private void OnDestroy() => Dispose();

        public void Dispose()
        {
            avatar?.Dispose();
            /*cts?.Cancel();
            cts?.Dispose();
            mutex?.Dispose();
            StopCoroutine(lastCoroutine);*/
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
            denoiser?.Dispose();
            if(opusHandler != null)
                opusHandler.OnEncoded -= OnOpusEncoded;
            if(cts != null && !cts.IsCancellationRequested)
                cts.Cancel();
            mutex?.Dispose();
        }
    }
}