using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK;
using Hypernex.CCK.Unity;
using Hypernex.Networking;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using HypernexSharp.SocketObjects;
using Nexbox;
using Nexport;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hypernex.Game
{
    public class GameInstance : IDisposable
    {
        public static GameInstance FocusedInstance { get; internal set; }
        public static Action<GameInstance, WorldMeta> OnGameInstanceLoaded { get; set; } = (instance, meta) => { };

        internal static void Init()
        {
            // main thread
            SocketManager.OnInstanceJoined += (instance, meta) =>
            {
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

        public string gameServerId;
        public string instanceId;
        public string userIdToken;
        public WorldMeta worldMeta;
        public World World;
        public User host;

        private HypernexInstanceClient client;
        internal Scene loadedScene;
        internal bool authed;
        private List<Sandbox> sandboxes = new ();

        private GameInstance(JoinedInstance joinInstance, WorldMeta worldMeta)
        {
            gameServerId = joinInstance.gameServerId;
            instanceId = joinInstance.instanceId;
            userIdToken = joinInstance.tempUserToken;
            this.worldMeta = worldMeta;
            string[] s = joinInstance.Uri.Split(':');
            string ip = s[0];
            int port = Convert.ToInt32(s[1]);
            InstanceProtocol instanceProtocol = joinInstance.InstanceProtocol;
            ClientSettings clientSettings = new ClientSettings(ip, port, true);
            client = new HypernexInstanceClient(APIPlayer.APIObject, APIPlayer.APIUser, instanceProtocol,
                clientSettings);
            client.OnConnect += () =>
            {
                QuickInvoke.InvokeActionOnMainThread(OnConnect);
                APIPlayer.APIObject.GetUser(r => OnUser(r, joinInstance.instanceCreatorId),
                    joinInstance.instanceCreatorId, isUserId: true);
            };
            client.OnUserLoaded += user => QuickInvoke.InvokeActionOnMainThread(OnUserLoaded, user);
            client.OnClientConnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientConnect, user);
            client.OnMessage += (message, meta) => QuickInvoke.InvokeActionOnMainThread(OnMessage, message, meta);
            client.OnClientDisconnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientDisconnect, user);
            client.OnDisconnect += () =>
            {
                // Verify they actually leave the socket instance too
                SocketManager.LeaveInstance(gameServerId, instanceId);
                QuickInvoke.InvokeActionOnMainThread(OnDisconnect);
            };
            OnMessage += (meta, channel) => MessageHandler.HandleMessage(this, meta, channel);
            OnClientDisconnect += user => PlayerManagement.PlayerLeave(this, user);
            PlayerManagement.CreateGameInstance(this);
        }

        private GameInstance(InstanceOpened instanceOpened, WorldMeta worldMeta)
        {
            gameServerId = instanceOpened.gameServerId;
            instanceId = instanceOpened.instanceId;
            userIdToken = instanceOpened.tempUserToken;
            this.worldMeta = worldMeta;
            string[] s = instanceOpened.Uri.Split(':');
            string ip = s[0];
            int port = Convert.ToInt32(s[1]);
            InstanceProtocol instanceProtocol = instanceOpened.InstanceProtocol;
            ClientSettings clientSettings = new ClientSettings(ip, port, true);
            client = new HypernexInstanceClient(APIPlayer.APIObject, APIPlayer.APIUser, instanceProtocol,
                clientSettings);
            client.OnConnect += () => QuickInvoke.InvokeActionOnMainThread(OnConnect);
            client.OnUserLoaded += user => QuickInvoke.InvokeActionOnMainThread(OnUserLoaded, user);
            client.OnClientConnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientConnect, user);
            client.OnMessage += (message, meta) => QuickInvoke.InvokeActionOnMainThread(OnMessage, message, meta);
            client.OnClientDisconnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientDisconnect, user);
            client.OnDisconnect += () =>
            {
                // Verify they actually leave the socket instance too
                SocketManager.LeaveInstance(gameServerId, instanceId);
                QuickInvoke.InvokeActionOnMainThread(OnDisconnect);
            };
            OnMessage += (meta, channel) => MessageHandler.HandleMessage(this, meta, channel);
            OnClientDisconnect += user => PlayerManagement.PlayerLeave(this, user);
            PlayerManagement.CreateGameInstance(this);
        }

        private void OnUser(CallbackResult<GetUserResult> r, string hostId)
        {
            if (GameInstance.FocusedInstance != this)
                return;
            if (!r.success)
            {
                APIPlayer.APIObject.GetUser(rr => OnUser(rr, hostId), hostId, isUserId: true);
                return;
            }
            QuickInvoke.InvokeActionOnMainThread(new Action(() =>
            {
                host = r.result.UserData;
                DiscordTools.FocusInstance(worldMeta, gameServerId + "/" + instanceId, host);
            }));
        }

        public void Open()
        {
            if(!client.IsOpen)
                client.Open();
        }
        public void Close()
        {
            SceneManager.LoadScene(0);
            DiscordTools.UnfocusInstance(gameServerId + "/" + instanceId);
            PlayerManagement.DestroyGameInstance(this);
            client?.Stop();
        }

        /// <summary>
        /// Sends a message over the client. If this is multithreaded, DO NOT PASS UNITY OBJECTS.
        /// </summary>
        /// <param name="message">message to send</param>
        /// <param name="messageChannel">channel to send message over</param>
        public void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if(authed)
                client.SendMessage(message, messageChannel);
        }
        
        internal void __SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable) =>
            client.SendMessage(message, messageChannel);

        private IEnumerator LoadScene(bool open, string s)
        {
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(s, new LoadSceneParameters(LoadSceneMode.Single, LocalPhysicsMode.Physics3D));
            /*while (!asyncOperation.isDone)
                yield return null;*/
            yield return new WaitUntil(() => asyncOperation.isDone);
            Scene currentScene = SceneManager.GetSceneByPath(s);
            //SceneManager.MoveGameObjectToScene(
                //DontDestroyMe.GetNotDestroyedObject("Physics").GetComponent<DontDestroyMe>().Clone(), currentScene);
            foreach (GameObject rootGameObject in currentScene.GetRootGameObjects())
            {
                Transform[] ts = rootGameObject.GetComponentsInChildren<Transform>();
                foreach (Transform transform in ts)
                {
                    World w1 = transform.gameObject.GetComponent<World>();
                    if (w1 != null)
                        World = w1;
                    Camera c1 = transform.gameObject.GetComponent<Camera>();
                    if (c1 != null && c1.transform.parent != null && c1.transform.parent.gameObject.GetComponent<Mirror>() == null)
                    {
                        c1.gameObject.tag = "Untagged";
                        c1.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                    }
                    AudioListener a1 = transform.gameObject.GetComponent<AudioListener>();
                    if(a1 != null)
                        Object.Destroy(a1);
                    Canvas c2 = transform.gameObject.GetComponent<Canvas>();
                    if (c2 != null)
                        c2.worldCamera = LocalPlayer.Instance.Camera;
                }
            }
            LocalPlayer.Instance.DontDestroyMe.MoveToScene(currentScene);
            if (World == null)
                Dispose();
            else
            {
                loadedScene = currentScene;
                FocusedInstance = this;
                if(open)
                    Open();
                foreach (NexboxScript worldLocalScript in World.LocalScripts)
                    sandboxes.Add(new Sandbox(worldLocalScript, SandboxRestriction.Local, this));
                if (LocalPlayer.Instance.Dashboard.IsVisible)
                {
                    LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
                    LocalPlayer.Instance.LockCamera = false;
                    LocalPlayer.Instance.LockMovement = false;
                }
                yield return new WaitUntil(() => currentScene.isLoaded);
                Vector3 spawnPosition = Vector3.zero;
                if (World.SpawnPoints.Count > 0)
                {
                    Transform spT = World.SpawnPoints[new System.Random().Next(0, World.SpawnPoints.Count - 1)]
                        .transform;
                    spawnPosition = spT.position;
                }
                LocalPlayer.Instance.transform.position = spawnPosition;
                OnGameInstanceLoaded.Invoke(this, worldMeta);
            }
        }

        private void Load(bool open = true)
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
                    string s = AssetBundleTools.LoadSceneFromFile(o);
                    if (!string.IsNullOrEmpty(s))
                    {
                        CoroutineRunner.Instance.Run(LoadScene(open, s));
                    }
                    else
                        Dispose();
                }, knownHash);
            }, worldMeta.OwnerId, fileId);
        }

        public void Dispose()
        {
            Close();
            LocalPlayer.Instance.DontDestroyMe.Register();
            foreach (SandboxFunc sandboxAction in Runtime.OnUpdates)
                Runtime.RemoveOnUpdate(sandboxAction);
            foreach (Sandbox sandbox in sandboxes)
                sandbox.Dispose();
        }
    }
}