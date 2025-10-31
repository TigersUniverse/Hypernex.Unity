using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Networking;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.Sandboxing.SandboxedTypes.Handlers;
using Hypernex.Tools;
using Hypernex.Tools.Debug;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using HypernexSharp.SocketObjects;
using Nexport;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Physics = UnityEngine.Physics;
using Security = Hypernex.CCK.Unity.Internals.Security;
using World = Hypernex.CCK.Unity.Assets.World;

namespace Hypernex.Game
{
    public class GameInstance
    {
        public static GameInstance FocusedInstance { get; internal set; }

        private static List<GameInstance> gameInstances = new();
        public static GameInstance[] GameInstances => gameInstances.ToArray();

        public static Action<GameInstance, WorldMeta, Scene> OnGameInstanceLoaded { get; set; } =
            (instance, meta, scene) => { };
        public static Action<GameInstance> OnGameInstanceDisconnect { get; set; } = instance => { };
        public static Dictionary<WorldMeta, float> WorldDownloadProgress = new();

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

        internal static void HandleDownloadProgress(WorldMeta worldMeta, float progress)
        {
            if (WorldDownloadProgress.Count(x => x.Key.Id == worldMeta.Id) <= 0)
            {
                WorldDownloadProgress.Add(worldMeta, progress);
                return;
            }
            for (int i = 0; i < WorldDownloadProgress.Count; i++)
            {
                WorldMeta w = WorldDownloadProgress.ElementAt(i).Key;
                if(w.Id != worldMeta.Id) continue;
                WorldDownloadProgress[w] = progress;
            }
        }

        public static bool IsDownloading(string worldId) => WorldDownloadProgress.Count(x => x.Key.Id == worldId) > 0;
        public static bool IsDownloading(WorldMeta worldMeta) => IsDownloading(worldMeta.Id);
        
        public static float? GetDownloadProgress(string worldId)
        {
            for (int i = 0; i < WorldDownloadProgress.Count; i++)
            {
                KeyValuePair<WorldMeta, float> pair = WorldDownloadProgress.ElementAt(i);
                WorldMeta w = pair.Key;
                if(w.Id != worldId) continue;
                return pair.Value;
            }
            return null;
        }
        public static float? GetDownloadProgress(WorldMeta worldMeta) => GetDownloadProgress(worldMeta.Id);

        public static (WorldMeta, float)[] GetAllDownloads()
        {
            int count = WorldDownloadProgress.Count;
            if (count <= 0) return Array.Empty< (WorldMeta, float)>();
            return WorldDownloadProgress.Select(x => (x.Key, x.Value)).ToArray();
        }

        internal static void FinishDownload(WorldMeta worldMeta) => WorldDownloadProgress =
            WorldDownloadProgress.Where(x => x.Key.Id != worldMeta.Id).ToDictionary(x => x.Key, y => y.Value);
        
        public static User[] GetConnectedUsers(GameInstance gameInstance, bool includeLocal = true)
        {
            User[] justLocal = {APIPlayer.APIUser};
            if (gameInstance == null)
                return includeLocal ? justLocal : Array.Empty<User>();
            return includeLocal
                ? gameInstance.ConnectedUsers.Union(justLocal, Extensions.UserEqualityComparer.Instance).ToArray()
                : gameInstance.ConnectedUsers.ToArray();
        }

        public static GameInstance GetInstanceFromScene(Scene scene)
        {
            foreach (GameInstance gameInstance in GameInstances)
            {
                if(gameInstance.loadedScene != scene) continue;
                return gameInstance;
            }
            return null;
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
                        return true;
                    case InstancePublicity.ModeratorRequest:
                        return Moderators.Contains(APIPlayer.APIUser.Id);
                    case InstancePublicity.ClosedRequest:
                        return false;
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
        internal ScriptEvents LocalScriptEvents;
        internal ScriptEvents AvatarScriptEvents;
        private Volume[] volumes;

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
            host = APIPlayer.APIUser;
            SetupClient(ip, port, instanceProtocol);
        }

        private void SetupClient(string ip, int port, InstanceProtocol instanceProtocol)
        {
            HandleCamera.DisposeAll();
            LocalScriptEvents = new ScriptEvents(SandboxRestriction.Local);
            AvatarScriptEvents = new ScriptEvents(SandboxRestriction.LocalAvatar);
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
            OnClientConnect += user =>
            {
                LocalScriptEvents?.OnUserJoin.Invoke(user.Id);
                AvatarScriptEvents?.OnUserJoin.Invoke(user.Id);
            };
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
        public void SendMessage(string name, byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if (!authed || !IsOpen) return;
            client.SendMessage(message, messageChannel);
            if(MessagePackListener.Instance == null) return;
            MessagePackListener.Instance.AddMessage(name, message.Length);
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
            SendMessage(typeof(WarnPlayer).FullName, Msg.Serialize(warnPlayer));
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
            SendMessage(typeof(KickPlayer).FullName, Msg.Serialize(kickPlayer));
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
            SendMessage(typeof(BanPlayer).FullName, Msg.Serialize(banPlayer));
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
            SendMessage(typeof(UnbanPlayer).FullName, Msg.Serialize(unbanPlayer));
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
            SendMessage(typeof(AddModerator).FullName, Msg.Serialize(addModerator));
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
            SendMessage(typeof(RemovedModerator).FullName, Msg.Serialize(removeModerator));
        }

        internal void __SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if(IsOpen)
            {
                client.SendMessage(message, messageChannel);
                if(MessagePackListener.Instance == null) return;
                MessagePackListener.Instance.AddMessage(typeof(JoinAuth).FullName, message.Length);
            }
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
#if UNITY_MAC || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                OSTools.ReplaceAllShaders(currentScene);
#endif
                Security.RemoveOffendingItems(currentScene, SecurityTools.AdditionalAllowedWorldTypes.ToArray());
                Security.ApplyComponentRestrictions(currentScene);
                try
                {
                    World = Object.FindObjectsOfType<World>().First(x => x.gameObject.scene == currentScene);
                } catch(Exception){}
                if (World == null)
                    Dispose();
                else
                {
                    loadedScene = currentScene;
                    FocusedInstance = this;
                    if (open)
                        Open();
                    foreach (LocalScript ls in Object.FindObjectsByType<LocalScript>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        Transform r = AnimationUtility.GetRootOfChild(ls.transform);
                        if(r.GetComponent<LocalPlayer>() == null && r.GetComponent<NetPlayer>() == null)
                            sandboxes.Add(new Sandbox(ls.Script, this, ls.gameObject));
                    }
                    if (LocalPlayer.Instance.Dashboard.IsVisible)
                        LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
                }
            }, currentScene =>
            {
                LocalPlayer.Instance.Respawn();
                volumes = Object.FindObjectsOfType<Volume>(true).Where(x => x.gameObject.scene == currentScene)
                    .ToArray();
                OnGameInstanceLoaded.Invoke(this, worldMeta, currentScene);
                gameInstances.Add(this);
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
                        FinishDownload(worldMeta);
                        CoroutineRunner.Instance.Run(AssetBundleTools.LoadSceneFromFile(o, s =>
                        {
                            if (!string.IsNullOrEmpty(s))
                                LoadScene(open, s);
                            else
                                Dispose();
                        }, this));
                    }, knownHash, args => HandleDownloadProgress(worldMeta, args.ProgressPercentage / 100f));
                }, worldMeta.OwnerId, fileId);
            }
        }

        internal void FixedUpdate() => sandboxes.ForEach(x => x.InstanceContainer.Runtime.FixedUpdate());

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
            }
            if(host != null)
                DiscordTools.FocusInstance(worldMeta, gameServerId + "/" + instanceId, host);
            sandboxes.ForEach(x => x.InstanceContainer.Runtime.Update());
            volumes.SelectVolume();
        }
        
        internal void LateUpdate() => sandboxes.ForEach(x => x.InstanceContainer.Runtime.LateUpdate());

        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            FocusedInstance = null;
            Physics.gravity = new Vector3(0, LocalPlayer.Instance.Gravity, 0);
            sandboxes.ForEach(x => x.Dispose());
            sandboxes.Clear();
            HandleCamera.DisposeAll();
            Close();
            DynamicGI.UpdateEnvironment();
            Array.Empty<Volume>().SelectVolume();
            VolumeManager.instance.SetGlobalDefaultProfile(global::Init.Instance.DefaultVolumeProfile);
            if(gameInstances.Contains(this))
                gameInstances.Remove(this);
            OnGameInstanceDisconnect.Invoke(this);
        }
    }
}