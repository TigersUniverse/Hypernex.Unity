using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.Networking;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using HypernexSharp.SocketObjects;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SceneSystem;
using Nexport;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hypernex.Game
{
    public class GameInstance : IDisposable
    {
        private static readonly List<GameInstance> gameInstances = new();
        public static List<GameInstance> GameInstances => new(gameInstances);
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
        }

        public Action OnConnect { get; set; } = () => { };
        public Action<User> OnClientConnect { get; set; } = identifier => { };
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
            client.OnClientConnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientConnect, user);
            client.OnMessage += (message, meta) => QuickInvoke.InvokeActionOnMainThread(OnMessage, message, meta);
            client.OnClientDisconnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientDisconnect, user);
            client.OnDisconnect += () =>
            {
                // Verify they actually leave the socket instance too
                SocketManager.LeaveInstance(gameServerId, instanceId);
                QuickInvoke.InvokeActionOnMainThread(OnDisconnect);
            };
            gameInstances.Add(this);
            OnMessage += (meta, channel) => MessageHandler.HandleMessage(this, meta, channel);
            OnClientDisconnect += user => PlayerManagement.PlayerLeave(this, user.Id);
        }

        private void OnUser(CallbackResult<GetUserResult> r, string hostId)
        {
            if (!GameInstances.Contains(this))
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
            if(GameInstances.Contains(this) && !client.IsOpen)
                client.Open();
        }
        public void Close()
        {
            client.Close();
            gameInstances.Remove(this);
            SceneManager.LoadScene(0);
            DiscordTools.UnfocusInstance(gameServerId + "/" + instanceId);
        }
        /// <summary>
        /// Sends a message over the client. If this is multithreaded, DO NOT PASS UNITY OBJECTS.
        /// </summary>
        /// <param name="message">message to send</param>
        /// <param name="messageChannel">channel to send message over</param>
        public void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable) =>
            client.SendMessage(message, messageChannel);

        private async void LoadScene(bool open, Scene s)
        {
            IMixedRealitySceneSystem sceneSystem =
                MixedRealityToolkit.Instance.GetService<IMixedRealitySceneSystem>();
            await sceneSystem.LoadContent(s.name, LoadSceneMode.Single);
            //SceneManager.LoadScene(s.Value.buildIndex);
            foreach (GameObject rootGameObject in s.GetRootGameObjects())
            {
                World[] t = rootGameObject.GetComponentsInChildren<World>(true);
                if (t.Length > 0)
                {
                    World = t[0];
                    break;
                }
            }
            if (World == null)
            {
                Dispose();
                return;
            }
            loadedScene = s;
            FocusedInstance = this;
            if(open)
                Open();
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
            DownloadTools.DownloadFile(fileURL, $"{worldMeta.Id}.hnw", o =>
            {
                Scene? s = AssetBundleTools.LoadSceneFromFile(o);
                if (s != null)
                {
                    LoadScene(open, s.Value);
                }
                else
                    Dispose();
            });
        }

        public void Dispose()
        {
            Close();
            if (GameInstances.Contains(this))
                gameInstances.Remove(this);
        }
    }
}