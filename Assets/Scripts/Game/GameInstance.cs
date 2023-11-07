using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.CCK;
using Hypernex.CCK.Unity;
using Hypernex.Networking;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using HypernexSharp.SocketObjects;
using Nexport;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Object = UnityEngine.Object;
using Physics = UnityEngine.Physics;

namespace Hypernex.Game
{
    public class GameInstance
    {
        public static GameInstance FocusedInstance { get; internal set; }
        public static Action<GameInstance, WorldMeta> OnGameInstanceLoaded { get; set; } = (instance, meta) => { };

        internal static void Init()
        {
            // main thread
            SocketManager.OnInstanceJoined += (instance, meta) =>
            {
                if (FocusedInstance != null && FocusedInstance.gameServerId == instance.gameServerId &&
                    FocusedInstance.instanceId == instance.instanceId)
                {
                    // Reconnect Socket
                    return;
                }
                GameInstance gameInstance = new GameInstance(instance, meta);
                gameInstance.Load();
            };
            SocketManager.OnInstanceOpened += (opened, meta) =>
            {
                GameInstance gameInstance = new GameInstance(opened, meta);
                gameInstance.Load();
            };
        }

        public Action OnConnect { get; set; } = () => { };
        public Action<User> OnUserLoaded { get; set; } = user => { };
        public Action<User> OnClientConnect { get; set; } = user => { };
        public Action<MsgMeta, MessageChannel> OnMessage { get; set; } = (meta, channel) => { };
        public Action<User> OnClientDisconnect { get; set; } = identifier => { };
        public Action OnDisconnect { get; set; } = () => { };

        public bool IsOpen => client?.IsOpen ?? false;
        public List<User> ConnectedUsers => client.ConnectedUsers;
        
        public bool CanInvite
        {
            get
            {
                if (instanceCreatorId == APIPlayer.APIUser.Id)
                    return true;
                switch (Publicity)
                {
                    case InstancePublicity.Anyone:
                        return true;
                    case InstancePublicity.Acquaintances:
                    case InstancePublicity.Friends:
                    case InstancePublicity.OpenRequest:
                        if (host == null)
                            return false;
                        return host.Friends.Contains(APIPlayer.APIUser.Id);
                    case InstancePublicity.ModeratorRequest:
                        return Moderators.Contains(APIPlayer.APIUser.Id);
                }
                return false;
            }
        }

        public bool IsModerator => Moderators.Contains(APIPlayer.APIUser.Id);

        public List<string> Moderators = new();
        public List<string> BannedUsers = new();
        public List<string> SocketConnectedUsers = new();

        public string gameServerId;
        public string instanceId;
        public string userIdToken;
        public WorldMeta worldMeta;
        public World World;
        public User host;
        public Texture2D Thumbnail;
        public InstancePublicity Publicity;
        public bool lockAvatarSwitching;

        private HypernexInstanceClient client;
        internal Scene loadedScene;
        internal bool authed;
        internal List<Sandbox> sandboxes = new ();
        public readonly string instanceCreatorId;

        private string hostId => client?.HostId ?? String.Empty;
        internal bool isHost
        {
            get
            {
                if (APIPlayer.APIUser == null)
                    return false;
                if (client == null)
                    return false;
                return client.HostId == APIPlayer.APIUser.Id;
            }
        }
        private List<User> usersBeforeMe = new ();
        private bool isDisposed;
        internal ScriptEvents ScriptEvents;

        private GameInstance(JoinedInstance joinInstance, WorldMeta worldMeta)
        {
            FocusedInstance?.Dispose();
            gameServerId = joinInstance.gameServerId;
            instanceId = joinInstance.instanceId;
            userIdToken = joinInstance.tempUserToken;
            this.worldMeta = worldMeta;
            instanceCreatorId = joinInstance.instanceCreatorId;
            Publicity = joinInstance.InstancePublicity;
            Moderators = joinInstance.Moderators;
            BannedUsers = joinInstance.BannedUsers;
            string[] s = joinInstance.Uri.Split(':');
            string ip = s[0];
            int port = Convert.ToInt32(s[1]);
            InstanceProtocol instanceProtocol = joinInstance.InstanceProtocol;
            SetupClient(ip, port, instanceProtocol);
        }

        private GameInstance(InstanceOpened instanceOpened, WorldMeta worldMeta)
        {
            FocusedInstance?.Dispose();
            gameServerId = instanceOpened.gameServerId;
            instanceId = instanceOpened.instanceId;
            userIdToken = instanceOpened.tempUserToken;
            this.worldMeta = worldMeta;
            instanceCreatorId = APIPlayer.APIUser.Id;
            Publicity = instanceOpened.InstancePublicity;
            Moderators = instanceOpened.Moderators;
            BannedUsers = instanceOpened.BannedUsers;
            string[] s = instanceOpened.Uri.Split(':');
            string ip = s[0];
            int port = Convert.ToInt32(s[1]);
            InstanceProtocol instanceProtocol = instanceOpened.InstanceProtocol;
            SetupClient(ip, port, instanceProtocol);
        }

        private void SetupClient(string ip, int port, InstanceProtocol instanceProtocol)
        {
            ScriptEvents = new ScriptEvents(this);
            ClientSettings clientSettings = new ClientSettings(ip, port, instanceProtocol == InstanceProtocol.UDP, 1);
            client = new HypernexInstanceClient(APIPlayer.APIObject, APIPlayer.APIUser, instanceProtocol,
                clientSettings);
            client.OnConnect += () =>
            {
                QuickInvoke.InvokeActionOnMainThread(OnConnect);
            };
            client.OnUserLoaded += user => QuickInvoke.InvokeActionOnMainThread(OnUserLoaded, user);
            client.OnClientConnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientConnect, user);
            client.OnMessage += (message, meta) => QuickInvoke.InvokeActionOnMainThread(OnMessage, message, meta);
            client.OnClientDisconnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientDisconnect, user);
            client.OnDisconnect += () =>
            {
                if (isDisposed)
                    return;
                // Verify they actually leave the socket instance too
                SocketManager.LeaveInstance(gameServerId, instanceId);
                QuickInvoke.InvokeActionOnMainThread(OnDisconnect);
            };
            OnClientConnect += user => ScriptEvents?.OnUserJoin.Invoke(user.Id);
            OnMessage += (meta, channel) => MessageHandler.HandleMessage(this, meta, channel);
            OnClientDisconnect += user => PlayerManagement.PlayerLeave(this, user);
            OnDisconnect += Dispose;
            PlayerManagement.CreateGameInstance(this);
            APIPlayer.UserSocket.OnOpen += () =>
            {
                // Socket probably reconnected, rejoin instance
                SocketManager.JoinInstance(new SafeInstance
                {
                    GameServerId = gameServerId,
                    InstanceId = instanceId
                });
            };
            APIPlayer.OnLogout += Dispose;
        }

        public void Open()
        {
            if(!client.IsOpen)
                client.Open();
        }
        public void Close()
        {
            if(!global::Init.IsQuitting)
                CoroutineRunner.Instance.Run(LocalPlayer.Instance.SafeSwitchScene(1, null,
                    s =>
                    {
                        LocalPlayer.Instance.Respawn(s);
                        LocalPlayer.Instance.Dashboard.PositionDashboard(LocalPlayer.Instance);
                    }));
            DiscordTools.UnfocusInstance(gameServerId + "/" + instanceId);
            PlayerManagement.DestroyGameInstance(this);
            if(IsOpen)
                client?.Stop();
        }

        /// <summary>
        /// Sends a message over the client. If this is multithreaded, DO NOT PASS UNITY OBJECTS.
        /// </summary>
        /// <param name="message">message to send</param>
        /// <param name="messageChannel">channel to send message over</param>
        public void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if(authed && IsOpen)
                client.SendMessage(message, messageChannel);
        }

        public void InviteUser(User user)
        {
            if (!CanInvite)
                return;
            SocketManager.InviteUser(this, user);
        }

        public void WarnUser(User user, string message)
        {
            if(!IsModerator)
                return;
            WarnPlayer warnPlayer = new WarnPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id,
                message = message
            };
            SendMessage(Msg.Serialize(warnPlayer));
        }

        public void KickUser(User user, string message)
        {
            if (!IsModerator)
                return;
            KickPlayer kickPlayer = new KickPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id,
                message = message
            };
            SendMessage(Msg.Serialize(kickPlayer));
        }

        public void BanUser(User user, string message)
        {
            if (!IsModerator)
                return;
            BanPlayer banPlayer = new BanPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id,
                message = message
            };
            SendMessage(Msg.Serialize(banPlayer));
        }

        public void UnbanUser(User user)
        {
            if (!IsModerator)
                return;
            UnbanPlayer unbanPlayer = new UnbanPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id
            };
            SendMessage(Msg.Serialize(unbanPlayer));
        }

        public void AddModerator(User user)
        {
            if (!IsModerator)
                return;
            AddModerator addModerator = new AddModerator
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id
            };
            SendMessage(Msg.Serialize(addModerator));
        }
        
        public void RemoveModerator(User user)
        {
            if (!IsModerator)
                return;
            RemoveModerator removeModerator = new RemoveModerator
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id
            };
            SendMessage(Msg.Serialize(removeModerator));
        }

        internal void __SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if(IsOpen)
                client.SendMessage(message, messageChannel);
        }

        internal void UpdateInstanceMeta(UpdatedInstance updatedInstance)
        {
            Moderators = new List<string>(updatedInstance.instanceMeta.Moderators);
            BannedUsers = new List<string>(updatedInstance.instanceMeta.BannedUsers);
            SocketConnectedUsers = new List<string>(updatedInstance.instanceMeta.ConnectedUsers);
        }

        private void LoadScene(bool open, string s) => CoroutineRunner.Instance.Run(
            LocalPlayer.Instance.SafeSwitchScene(s, currentScene =>
            {
                foreach (GameObject rootGameObject in currentScene.GetRootGameObjects())
                {
                    Transform[] ts = rootGameObject.GetComponentsInChildren<Transform>(true);
                    foreach (Transform transform in ts)
                    {
                        World w1 = transform.gameObject.GetComponent<World>();
                        if (w1 != null)
                            World = w1;
                        Camera c1 = transform.gameObject.GetComponent<Camera>();
                        if (c1 != null && c1.transform.parent != null &&
                            c1.transform.parent.gameObject.GetComponent<Mirror>() == null)
                        {
                            c1.gameObject.tag = "Untagged";
                            c1.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                        }

                        AudioListener a1 = transform.gameObject.GetComponent<AudioListener>();
                        if (a1 != null)
                            Object.Destroy(a1);
                        Canvas c2 = transform.gameObject.GetComponent<Canvas>();
                        if (c2 != null)
                        {
                            c2.worldCamera = LocalPlayer.Instance.Camera;
                            if (c2.renderMode == RenderMode.WorldSpace)
                            {
                                TrackedDeviceGraphicRaycaster trackedDeviceGraphicRaycaster =
                                    c2.gameObject.GetComponent<TrackedDeviceGraphicRaycaster>();
                                if (trackedDeviceGraphicRaycaster == null)
                                    c2.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                                /*trackedDeviceGraphicRaycaster.checkFor3DOcclusion = true;
                                trackedDeviceGraphicRaycaster.blockingMask = ~0;*/
                            }
                        }
                        NetworkSyncDescriptor networkSyncDescriptor = transform.gameObject.GetComponent<NetworkSyncDescriptor>();
                        if (networkSyncDescriptor != null)
                        {
                            NetworkSync networkSync = networkSyncDescriptor.gameObject.AddComponent<NetworkSync>();
                            networkSync.InstanceHostOnly = networkSyncDescriptor.InstanceHostOnly;
                            networkSync.CanSteal = networkSyncDescriptor.CanSteal;
                            networkSync.AlwaysSync = networkSyncDescriptor.AlwaysSync;
                            if(networkSyncDescriptor.InstanceHostOnly && isHost)
                                networkSync.Claim();
                        }
                        GrabbableDescriptor grabbableDescriptor =
                            transform.gameObject.GetComponent<GrabbableDescriptor>();
                        if (grabbableDescriptor != null)
                        {
                            Grabbable grabbable = grabbableDescriptor.gameObject.AddComponent<Grabbable>();
                            grabbable.ApplyVelocity = grabbableDescriptor.ApplyVelocity;
                            grabbable.VelocityAmount = grabbableDescriptor.VelocityAmount;
                            grabbable.VelocityThreshold = grabbableDescriptor.VelocityThreshold;
                            grabbable.GrabByLaser = grabbableDescriptor.GrabByLaser;
                            grabbable.LaserGrabDistance = grabbableDescriptor.LaserGrabDistance;
                            grabbable.GrabDistance = grabbableDescriptor.GrabDistance;
                            grabbable.GrabDistance = grabbableDescriptor.GrabDistance;
                        }
                        RespawnableDescriptor respawnableDescriptor =
                            transform.gameObject.GetComponent<RespawnableDescriptor>();
                        if (respawnableDescriptor != null)
                        {
                            Respawnable respawnable = respawnableDescriptor.gameObject.AddComponent<Respawnable>();
                            respawnable.LowestPointRespawnThreshold = respawnableDescriptor.LowestPointRespawnThreshold;
                        }
                        EventSystem eventSystem = transform.gameObject.GetComponent<EventSystem>();
                        if(eventSystem != null)
                            Object.Destroy(eventSystem.gameObject);
                        AudioSource[] audios = transform.gameObject.GetComponents<AudioSource>();
                        foreach (AudioSource audioSource in audios)
                        {
                            Transform root = AnimationUtility.GetRootOfChild(audioSource.transform);
                            if (root != null && (root.GetComponent<NetPlayer>() != null ||
                                                 root.GetComponent<LocalPlayer>() != null))
                                continue;
                            audioSource.outputAudioMixerGroup = global::Init.Instance.WorldGroup;
                        }
                        VideoPlayer[] videoPlayers = transform.gameObject.GetComponents<VideoPlayer>();
                        foreach (VideoPlayer videoPlayer in videoPlayers)
                        {
                            AudioSource audioSource = videoPlayer.gameObject.GetComponent<AudioSource>();
                            if (audioSource == null)
                                audioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
                            audioSource.outputAudioMixerGroup = global::Init.Instance.WorldGroup;
                            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                            videoPlayer.SetTargetAudioSource(0, audioSource);
                        }
                    }
                }
                if (World == null)
                    Dispose();
                else
                {
                    loadedScene = currentScene;
                    FocusedInstance = this;
                    if(string.IsNullOrEmpty(worldMeta.ThumbnailURL))
                        CurrentInstanceBanner.Instance.Render(this, Array.Empty<byte>());
                    else
                        DownloadTools.DownloadBytes(worldMeta.ThumbnailURL,
                            bytes =>
                            {
                                Thumbnail = ImageTools.BytesToTexture2D(worldMeta.ThumbnailURL, bytes);
                                CurrentInstanceBanner.Instance.Render(this, bytes);
                            });
                    if (open)
                        Open();
                    foreach (NexboxScript worldLocalScript in World.LocalScripts)
                        sandboxes.Add(new Sandbox(worldLocalScript, this, World.gameObject));
                    foreach (LocalScript ls in Object.FindObjectsByType<LocalScript>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        Transform r = AnimationUtility.GetRootOfChild(ls.transform);
                        if(r.GetComponent<LocalPlayer>() == null && r.GetComponent<NetPlayer>() == null)
                            sandboxes.Add(new Sandbox(ls.NexboxScript, this, ls.gameObject));
                    }
                    if (LocalPlayer.Instance.Dashboard.IsVisible)
                        LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
                }
            }, currentScene =>
            {
                LocalPlayer.Instance.Respawn();
                OnGameInstanceLoaded.Invoke(this, worldMeta);
            }));

        private void Load(bool open = true)
        {
            if (SocketManager.DownloadedWorlds.ContainsKey(worldMeta.Id) &&
                File.Exists(SocketManager.DownloadedWorlds[worldMeta.Id]))
            {
                string o = SocketManager.DownloadedWorlds[worldMeta.Id];
                CoroutineRunner.Instance.Run(AssetBundleTools.LoadSceneFromFile(o, s =>
                {
                    if (!string.IsNullOrEmpty(s))
                        LoadScene(open, s);
                    else
                        Dispose();
                }, this));
            }
            else
            {
                // Download World
                string fileId;
                try
                {
                    if (AssetBundleTools.Platform == BuildPlatform.Android)
                        fileId = worldMeta.Builds.First(x => x.BuildPlatform == BuildPlatform.Android).FileId;
                    else
                        fileId = worldMeta.Builds.First(x => x.BuildPlatform == BuildPlatform.Windows).FileId;
                }
                catch (InvalidOperationException)
                {
                    Dispose();
                    return;
                }
                string fileURL = $"{APIPlayer.APIObject.Settings.APIURL}file/{worldMeta.OwnerId}/{fileId}";
                APIPlayer.APIObject.GetFileMeta(fileMetaResult =>
                {
                    string knownHash = String.Empty;
                    if (fileMetaResult.success)
                        knownHash = fileMetaResult.result.FileMeta.Hash;
                    DownloadTools.DownloadFile(fileURL, $"{worldMeta.Id}.hnw", o =>
                    {
                        CoroutineRunner.Instance.Run(AssetBundleTools.LoadSceneFromFile(o, s =>
                        {
                            if (!string.IsNullOrEmpty(s))
                                LoadScene(open, s);
                            else
                                Dispose();
                        }, this));
                    }, knownHash);
                }, worldMeta.OwnerId, fileId);
            }
        }

        internal void FixedUpdate() => sandboxes.ForEach(x => x.Runtime.FixedUpdate());

        internal void Update()
        {
            if (!string.IsNullOrEmpty(hostId) && (host == null || (host != null && host.Id != hostId)))
            {
                if (hostId == APIPlayer.APIUser.Id)
                    host = APIPlayer.APIUser;
                else
                    foreach (User connectedUser in ConnectedUsers)
                    {
                        if (connectedUser.Id == hostId)
                        {
                            host = connectedUser;
                        }
                    }
                if(host != null)
                    DiscordTools.FocusInstance(worldMeta, gameServerId + "/" + instanceId, host);
            }
            sandboxes.ForEach(x => x.Runtime.Update());
        }
        
        internal void LateUpdate() => sandboxes.ForEach(x => x.Runtime.LateUpdate());

        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            FocusedInstance = null;
            Physics.gravity = new Vector3(0, LocalPlayer.Instance.Gravity, 0);
            sandboxes.ForEach(x => x.Dispose());
            sandboxes.Clear();
            Close();
        }
    }
}