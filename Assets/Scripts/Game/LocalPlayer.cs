using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.CCK;
using Hypernex.CCK.Unity;
using Hypernex.Configuration;
using Hypernex.ExtendedTracking;
using Hypernex.Game.Audio;
using Hypernex.Game.Avatar;
using Hypernex.Game.Avatar.FingerInterfacing;
using Hypernex.Game.Bindings;
using Hypernex.Game.Networking;
using Hypernex.Networking.Messages;
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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Logger = Hypernex.CCK.Logger;
using Mic = Hypernex.Tools.Mic;
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
        public List<XRInteractorLineVisual> XRRays = new ();
        public VRInputListener VRInputListener;
        public Vector3 LowestPoint;
        public float LowestPointRespawnThreshold = 50f;
        public CurrentAvatar CurrentAvatarDisplay;
        public LocalPlayerSyncController LocalPlayerSyncController;
        public IGestureIdentifier GestureIdentifier = FingerCalibration.DefaultGestures;
        public DesktopFingerCurler.Left LeftDesktopCurler = new();
        public DesktopFingerCurler.Right RightDesktopCurler = new();

        private Denoiser denoiser;
        private float verticalVelocity;
        private float groundedTimer;
        internal AvatarMeta avatarMeta;
        public LocalAvatarCreator avatar;
        private string avatarFile;
        internal List<PathDescriptor> SavedTransforms = new();
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
            //AvatarCreator lastAvatar = avatar;
            StartCoroutine(AssetBundleTools.LoadAvatarFromFile(file, a =>
            {
                if (a == null)
                {
                    //avatar = lastAvatar;
                    return;
                }
                avatarMeta = am;
                avatar?.Dispose();
                CurrentAvatarDisplay.SizeAvatar(1f);
                avatar = new LocalAvatarCreator(this, a, IsVR, am);
                foreach (NexboxScript localAvatarScript in avatar.Avatar.LocalAvatarScripts)
                    avatar.localAvatarSandboxes.Add(new Sandbox(localAvatarScript, transform,
                        avatar.Avatar.gameObject));
                foreach (LocalScript ls in avatar.Avatar.gameObject.GetComponentsInChildren<LocalScript>())
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

        private List<Coroutine> lastCoroutine = new();

        private void Start()
        {
            if (Instance != null)
            {
                Logger.CurrentLogger.Warn("LocalPlayer already exists!");
                Destroy(this);
                return;
            }
            Instance = this;
            LocalPlayerSyncController = new LocalPlayerSyncController(this, i => lastCoroutine.Add(StartCoroutine(i)));
            DontDestroyMe = gameObject.GetComponent<DontDestroyMe>();
            APIPlayer.OnUser += _ => LoadAvatar();
            CharacterController.minMoveDistance = 0;
            LockCamera = Dashboard.IsVisible;
            LockMovement = Dashboard.IsVisible;
            CreateDesktopBindings();
            Mic.OnClipReady += (s, clip, isEmpty) =>
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
                    PlayerVoice[] playerVoices = audioCodec.Encode(samples, clip, new JoinAuth
                    {
                        UserId = APIPlayer.APIUser.Id,
                        TempToken = GameInstance.FocusedInstance.userIdToken
                    });
                    foreach (var playerVoice in playerVoices)
                    {
                        GameInstance.FocusedInstance.SendMessage(typeof(PlayerVoice).FullName, Msg.Serialize(playerVoice), MessageChannel.UnreliableSequenced);
                    }
                }
                if(!isEmpty) avatar?.ApplyAudioClipToLipSync(samples);
            };
            GameInstance.OnGameInstanceLoaded += (instance, meta) =>
            {
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
                    lastCoroutine.ForEach(StopCoroutine);
                };
                if (avatarMeta == null) return;
                if (avatarMeta.Publicity == AvatarPublicity.OwnerOnly)
                    ShareAvatarTokenToConnectedUsersInInstance(avatarMeta);
            };
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
            {
                avatar?.SetCrouch(false);
                avatar?.SetCrawl(false);
                Init.Instance.StartVR();
            }
            if(avatar != null)
                RefreshAvatar();
        }

        private void CreateDesktopBindings()
        {
            Bindings.Add(new Bindings.Keyboard()
                .RegisterCustomKeyDownEvent(KeyCode.V, () =>
                {
                    if(Dashboard.IsVisible) return;
                    if(EventSystem.current.currentSelectedGameObject != null) return;
                    Instance.MicrophoneEnabled = !Instance.MicrophoneEnabled;
                })
                .RegisterCustomKeyDownEvent(KeyCode.C, () =>
                {
                    if(LockMovement || !groundedPlayer) return;
                    avatar?.SetCrouch(!avatar?.IsCrouched ?? false);
                })
                .RegisterCustomKeyDownEvent(KeyCode.X, () =>
                {
                    if(LockMovement || !groundedPlayer) return;
                    avatar?.SetCrawl(!avatar?.IsCrawling ?? false);
                }));
            Bindings.Add(new Bindings.Mouse());
            Bindings[1].Button2Click += () => Dashboard.ToggleDashboard(this);
        }

        internal void StartVR()
        {
            AlignVR(true, true);
            Bindings.ForEach(x =>
            {
                if(x.GetType() == typeof(Bindings.Keyboard))
                    ((Bindings.Keyboard) x).Dispose();
                else if(x.GetType() == typeof(Bindings.Mouse))
                    ((Bindings.Mouse) x).Dispose();
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

        internal IFingerCurler GetLeftHandCurler()
        {
            if (IsVR) return LeftHandGetter;
            return LeftDesktopCurler;
        }

        internal IFingerCurler GetRightHandCurler()
        {
            if (IsVR) return RightHandGetter;
            return RightDesktopCurler;
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
        private bool isRunning;
        private bool groundedPlayer;

        private (Vector3, bool, bool, Vector2)? HandleLeftBinding(IBinding binding, bool vr)
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
            isRunning = binding.Button2 && (!avatar?.IsCrawling ?? true) && (!avatar?.IsCrouched ?? true);
            s_ = isRunning ? RunSpeed : WalkSpeed;
            if (GameInstance.FocusedInstance != null)
                if(GameInstance.FocusedInstance.World != null)
                    if (!GameInstance.FocusedInstance.World.AllowRunning)
                        s_ = WalkSpeed;
            return (move * s_, binding.Button,
                binding.Up > 0.01f || binding.Down > 0.01f || binding.Left > 0.01f || binding.Right > 0.01f,
                new(binding.Right - binding.Left, binding.Up - binding.Down));
        }

        private (Vector3, bool, bool)? HandleRightBinding(IBinding binding)
        {
            if (!LockCamera && binding.Id == "Mouse" && !IsVR)
            {
                transform.Rotate(0, (binding.Left * -1 + binding.Right) * ((Bindings.Mouse)binding).Sensitivity, 0);
                rotx += -(binding.Up + binding.Down * -1) * ((Bindings.Mouse) binding).Sensitivity;
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

        private void FixedUpdate() => avatar?.FixedUpdate();

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
            CursorTools.ToggleMouseLock(vr || LockCamera);
            CursorTools.ToggleMouseVisibility(!vr);
            groundedPlayer = CharacterController.isGrounded;
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
            (Vector3, bool, bool, Vector2)? left_m = null;
            (Vector3, bool, bool)? right_m = null;
            foreach (IBinding binding in new List<IBinding>(Bindings))
            {
                binding.Update();
                bool g = !binding.IsLook;
                if (vr)
                    g = binding.IsLook;
                if (g)
                {
                    (Vector3, bool, bool, Vector2)? r = HandleLeftBinding(binding, vr);
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
            bool isJumping = false;
            if (right_m != null)
            {
                isJumping = right_m.Value.Item2 && (!avatar?.IsCrouched ?? false) && (!avatar?.IsCrawling ?? false);
                if (isJumping && groundedTimer > 0)
                {
                    groundedTimer = 0;
                    verticalVelocity += Mathf.Sqrt(JumpHeight * 2 * -Gravity);
                }
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
                avatar?.SetMove(left_m?.Item4 ?? Vector2.zero, isRunning);
                avatar?.SetIsGrounded(groundedPlayer);
                avatar?.SetRun(isRunning);
                avatar?.Jump(isJumping && !groundedPlayer);
            }
            else
            {
                avatar?.SetMove(Vector2.zero, false);
                avatar?.SetIsGrounded(true);
                avatar?.SetRun(false);
                avatar?.Jump(false);
            }
            bool isMoving = left_m?.Item3 ?? false;
            if (!IsVR) DesktopFingerCurler.Update(ref LeftDesktopCurler, ref RightDesktopCurler, GestureIdentifier);
            avatar?.Update(areTwoTriggersClicked(), FakeVRHead, LeftHandVRIKTarget, RightHandVRIKTarget,
                isMoving, this);
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
        }

        private void LateUpdate()
        {
            avatar?.LateUpdate(IsVR, Camera.transform, LockCamera, !avatar?.IsCrawling ?? true);
            if (ConfigManager.SelectedConfigUser != null && ConfigManager.SelectedConfigUser.UseFacialTracking &&
                FaceTrackingManager.HasInitialized)
            {
                avatar?.UpdateEyes(FaceTrackingManager.GetEyeWeights());
                avatar?.UpdateFace(FaceTrackingManager.GetFaceWeights());
            }
        }

        private void OnDestroy() => Dispose();

        public void Dispose()
        {
            avatar?.Dispose();
            foreach (IBinding binding in Bindings)
            {
                if(binding.GetType() == typeof(Bindings.Keyboard))
                    ((Bindings.Keyboard)binding).Dispose();
                if(binding.GetType() == typeof(Bindings.Mouse))
                    ((Bindings.Mouse)binding).Dispose();
            }
            lastCoroutine.ForEach(StopCoroutine);
            denoiser?.Dispose();
            LocalPlayerSyncController?.Dispose();
        }
    }
}