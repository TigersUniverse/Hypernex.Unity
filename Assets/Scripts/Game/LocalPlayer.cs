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
using Hypernex.Game.Audio;
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
#if MAGICACLOTH
using MagicaCloth2;
#endif
using Keyboard = Hypernex.Game.Bindings.Keyboard;
using Logger = Hypernex.CCK.Logger;
using Mic = Hypernex.Tools.Mic;
using Mouse = Hypernex.Game.Bindings.Mouse;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace Hypernex.Game
{
    [RequireComponent(typeof(DontDestroyMe))]
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

        private float _walkSpeed = 5f;
        public float WalkSpeed
        {
            get
            {
                if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                    return GameInstance.FocusedInstance.World.WalkSpeed;
                return _walkSpeed;
            }
            set => _walkSpeed = value;
        }

        private float _runSpeed = 10f;
        public float RunSpeed
        {
            get
            {
                if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                    return GameInstance.FocusedInstance.World.RunSpeed;
                return _runSpeed;
            }
            set => _runSpeed = value;
        }
        
        private float _jumpHeight = 1.0f;
        public float JumpHeight
        {
            get
            {
                if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                    return GameInstance.FocusedInstance.World.JumpHeight;
                return _jumpHeight;
            }
            set => _jumpHeight = value;
        }
        
        private float _gravity = -9.87f;
        public float Gravity
        {
            get
            {
                if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                    return GameInstance.FocusedInstance.World.Gravity;
                return _gravity;
            }
            set => _gravity = value;
        }
        
        public bool LockMovement { get; set; }
        public bool LockCamera { get; set; }

        public List<IBinding> Bindings = new();

        public CoroutineRunner CoroutineRunner;
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
        internal List<LocalPlayerSyncObject> SavedTransforms = new();
        internal Dictionary<string, string> OwnedAvatarIdTokens = new();
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
                        .FirstOrDefault(x => x.name.ToLower() == "spawn");
                else
                    searchSpawn = s.Value.GetRootGameObjects().FirstOrDefault(x => x.name.ToLower() == "Spawn");
                if (searchSpawn != null)
                    spawnPosition = searchSpawn.transform.position;
            }
            CharacterController.enabled = false;
            transform.position = spawnPosition;
            if(Dashboard.IsVisible)
                Dashboard.PositionDashboard(this);
            CharacterController.enabled = true;
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
                    avatar.localAvatarSandboxes.Add(new Sandbox(localAvatarScript, transform,
                        avatar.Avatar.gameObject));
                foreach (LocalScript ls in a.GetComponentsInChildren<LocalScript>())
                    avatar.localAvatarSandboxes.Add(new Sandbox(ls.NexboxScript, transform, ls.gameObject));
                avatarFile = file;
                // Why this doesn't clear old transforms? I don't know.
                SavedTransforms.Clear();
                foreach (Transform child in avatar.Avatar.transform.GetComponentsInChildren<Transform>(true))
                {
                    PathDescriptor pathDescriptor = child.gameObject.GetComponent<PathDescriptor>();
                    if (pathDescriptor == null)
                        pathDescriptor = child.gameObject.AddComponent<PathDescriptor>();
                    pathDescriptor.root = transform;
                    LocalPlayerSyncObject localPlayerSyncObject =
                        child.gameObject.GetComponent<LocalPlayerSyncObject>();
                    if (localPlayerSyncObject == null)
                        localPlayerSyncObject = child.gameObject.AddComponent<LocalPlayerSyncObject>();
                    SavedTransforms.Add(localPlayerSyncObject);
                }
#if DYNAMIC_BONE
                foreach (DynamicBone dynamicBone in avatar.Avatar.transform.GetComponentsInChildren<DynamicBone>())
                {
                    dynamicBone.m_UpdateMode = DynamicBone.UpdateMode.AnimatePhysics;
                    dynamicBone.m_Roots.ForEach(x =>
                    {
                        if (x == null) return;
                        LocalPlayerSyncObject lp = x.GetComponent<LocalPlayerSyncObject>();
                        if(lp != null)
                            lp.MakeSpecial(dynamicBone);
                    });
                    if(dynamicBone.m_Root == null) continue;
                    LocalPlayerSyncObject localPlayerSyncObject =
                        dynamicBone.m_Root.GetComponent<LocalPlayerSyncObject>();
                    if(localPlayerSyncObject != null)
                        localPlayerSyncObject.MakeSpecial(dynamicBone);
                }
#endif
#if MAGICACLOTH
                foreach (MagicaCloth magicaCloth in avatar.Avatar.transform.GetComponentsInChildren<MagicaCloth>())
                {
                    // i don't *think* this could be null, but I'm not sure
                    if(magicaCloth.SerializeData != null)
                    {
                        magicaCloth.SerializeData.updateMode = ClothUpdateMode.Normal;
                        foreach (Transform rootBone in magicaCloth.SerializeData.rootBones)
                        {
                            if(rootBone == null) continue;
                            LocalPlayerSyncObject localPlayerSyncObject =
                                rootBone.GetComponent<LocalPlayerSyncObject>();
                            if (localPlayerSyncObject != null)
                                localPlayerSyncObject.MakeSpecial();
                        }
                    }
                }
#endif
                avatar.Avatar.gameObject.GetComponent<LocalPlayerSyncObject>().AlwaysSync = true;
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
        private List<WeightedObjectUpdate> weightedObjectUpdates = new();
        private Queue<PlayerObjectUpdate> poumsgs = new();
        private Queue<WeightedObjectUpdate> woumsgs = new();
        private CancellationTokenSource cts;
        private Mutex mutex = new();

        public void UpdateObject(NetworkedObject networkedObject)
        {
            if (GameInstance.FocusedInstance == null || !GameInstance.FocusedInstance.IsOpen) return;
            if(mutex.WaitOne(1))
            {
                PlayerObjectUpdate playerObjectUpdate = new PlayerObjectUpdate
                {
                    Auth = new JoinAuth
                    {
                        UserId = APIPlayer.APIUser.Id,
                        TempToken = GameInstance.FocusedInstance.userIdToken
                    },
                    Object = networkedObject
                };
                poumsgs.Enqueue(playerObjectUpdate);
                mutex.ReleaseMutex();
            }
        }

        public void UpdateWeight(WeightedObjectUpdate weightedObjectUpdate)
        {
            if (GameInstance.FocusedInstance == null || !GameInstance.FocusedInstance.IsOpen) return;
            if (mutex.WaitOne(1))
            {
                woumsgs.Enqueue(weightedObjectUpdate);
                mutex.ReleaseMutex();
            }
        }

        private IEnumerator UpdatePlayer(GameInstance gameInstance)
        {
            while (true)
            {
                if (gameInstance.IsOpen)
                {
                    PlayerUpdate playerUpdate = GetPlayerUpdate(gameInstance);
                    gameInstance.SendMessage(Msg.Serialize(playerUpdate), MessageChannel.Unreliable);
                }
                yield return new WaitForSeconds(MESSAGE_UPDATE_TIME);
            }
        }

        private void Start()
        {
            if (Instance != null)
            {
                Logger.CurrentLogger.Warn("LocalPlayer already exists!");
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyMe = gameObject.GetComponent<DontDestroyMe>();
            APIPlayer.OnUser += _ => LoadAvatar();
            CharacterController.minMoveDistance = 0;
            LockCamera = Dashboard.IsVisible;
            LockMovement = Dashboard.IsVisible;
            CreateDesktopBindings();
            Mic.OnClipReady += (s, clip) =>
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
                /*if (GameInstance.FocusedInstance != null && ConfigManager.SelectedConfigUser != null &&
                    ConfigManager.SelectedConfigUser.AudioCompression == AudioCompression.Opus && opusHandler != null)
                    opusHandler.EncodeMicrophone(samples);
                else if(GameInstance.FocusedInstance != null && ConfigManager.SelectedConfigUser != null && 
                        ConfigManager.SelectedConfigUser.AudioCompression == AudioCompression.RAW)
                    OnAudioRAW(samples, samples.Length);*/
                if (GameInstance.FocusedInstance != null)
                {
                    IAudioCodec audioCodec = null;
                    if(ConfigManager.SelectedConfigUser != null)
                        audioCodec = AudioSourceDriver.GetAudioCodecByName(
                            ConfigManager.SelectedConfigUser.AudioCompression.ToString());
                    if (audioCodec == null)
                        audioCodec = AudioSourceDriver.AudioCodecs[0];
                    PlayerVoice playerVoice = audioCodec.Encode(samples, clip, new JoinAuth
                    {
                        UserId = APIPlayer.APIUser.Id,
                        TempToken = GameInstance.FocusedInstance.userIdToken
                    });
                    GameInstance.FocusedInstance.SendMessage(Msg.Serialize(playerVoice));
                }
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

        public void SwitchVR()
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
            CoroutineRunner.Run(PositionDashboardOnVRSwitch());
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
            move = Vector3.ClampMagnitude(move, 1);
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
            {
                List<WeightedObjectUpdate> w = avatar?.GetAnimatorWeights(new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = GameInstance.FocusedInstance.userIdToken
                });
                if(w != null)
                    if (w.Count != weightedObjectUpdates.Count)
                    {
                        ResetWeightedObjects resetWeightedObjects = new ResetWeightedObjects
                        {
                            Auth = new JoinAuth
                            {
                                UserId = APIPlayer.APIUser.Id,
                                TempToken = GameInstance.FocusedInstance.userIdToken
                            }
                        };
                        GameInstance.FocusedInstance.SendMessage(Msg.Serialize(resetWeightedObjects));
                        weightedObjectUpdates.Clear();
                        w.ForEach(x =>
                        {
                            weightedObjectUpdates.Add(x);
                            UpdateWeight(x);
                        });
                    }
                    else
                    {
                        for (int x = 0; x < w.Count; x++)
                        {
                            WeightedObjectUpdate recent = w.ElementAt(x);
                            try
                            {
                                WeightedObjectUpdate cached = weightedObjectUpdates.First(b =>
                                    b.TypeOfWeight == recent.TypeOfWeight &&
                                    b.PathToWeightContainer == recent.PathToWeightContainer &&
                                    b.WeightIndex == recent.WeightIndex);
                                if (recent.Weight != cached.Weight)
                                {
                                    int y = weightedObjectUpdates.IndexOf(cached);
                                    weightedObjectUpdates[y] = recent;
                                    UpdateWeight(recent);
                                }
                            }
                            catch(Exception){}
                        }
                    }
            }
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
            if(cts != null && !cts.IsCancellationRequested)
                cts.Cancel();
            mutex?.Dispose();
        }
    }
}