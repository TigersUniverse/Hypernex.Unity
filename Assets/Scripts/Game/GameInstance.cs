using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Networking;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using HypernexSharp.SocketObjects;
using Nexport;
using UnityEngine.SceneManagement;

namespace Hypernex.Game
{
    public class GameInstance
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
            client.OnConnect += () => QuickInvoke.InvokeActionOnMainThread(OnConnect);
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

        public void Open()
        {
            if(GameInstances.Contains(this) && !client.IsOpen)
                client.Open();
        }
        public void Close()
        {
            client.Close();
            gameInstances.Remove(this);
        }
        /// <summary>
        /// Sends a message over the client. If this is multithreaded, DO NOT PASS UNITY OBJECTS.
        /// </summary>
        /// <param name="message">message to send</param>
        /// <param name="messageChannel">channel to send message over</param>
        public void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable) =>
            client.SendMessage(message, messageChannel);

        private void Load(bool open = true)
        {
            // Download World
            string fileId;
            if (AssetBundleTools.Platform == BuildPlatform.Android)
                fileId = worldMeta.Builds.First(x => x.BuildPlatform == BuildPlatform.Android).FileId;
            else
                fileId = worldMeta.Builds.First(x => x.BuildPlatform == BuildPlatform.Windows).FileId;
            string fileURL = $"{APIPlayer.APIObject.Settings.APIURL}file/{worldMeta.OwnerId}/{fileId}";
            DownloadTools.DownloadFile(fileURL, $"{worldMeta.Id}.hnw", o =>
            {
                Scene? s = AssetBundleTools.LoadSceneFromFile(o);
                if (s != null)
                {
                    SceneManager.LoadScene(s.Value.buildIndex);
                    loadedScene = s.Value;
                    FocusedInstance = this;
                    if(open)
                        Open();
                }
                else
                    Close();
            });
        }
    }
}