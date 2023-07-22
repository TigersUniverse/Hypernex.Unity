using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using HypernexSharp.SocketObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Templates
{
    public class CreateInstanceTemplate : MonoBehaviour
    {
        public TMP_Text WorldName;
        public TMP_Text WorldCreator;
        public TMP_Text InstancePublicityLabel;
        public TMP_Text InstanceProtocolLabel;
        public TMP_Text GameServersLabel;
        public DynamicScroll GameServersDisplay;
        public GameObject CreateButton;

        private InstancePublicity instancePublicity = InstancePublicity.Anyone;
        private InstanceProtocol instanceProtocol = InstanceProtocol.KCP;
        private WorldMeta worldMeta;
        private GameServer SelectedGameServer;
        private static List<GameServer> LastGameServers = new();

        public void Render(WorldMeta WorldMeta, User creator)
        {
            WorldName.text = WorldMeta.Name;
            WorldCreator.text = "World By " + creator.Username;
            worldMeta = WorldMeta;
            CreateButton.SetActive(false);
            LastGameServers.Clear();
            gameObject.SetActive(true);
            GameServersLabel.text = "Loading GameServers, please wait...";
            APIPlayer.APIObject.GetGameServers(r =>
                QuickInvoke.InvokeActionOnMainThread(new Action(() => OnGameServersResult(r))));
        }

        public void Anyone() => instancePublicity = InstancePublicity.Anyone;
        public void Acquaintances() => instancePublicity = InstancePublicity.Acquaintances;
        public void Friends() => instancePublicity = InstancePublicity.Friends;
        public void OpenRequest() => instancePublicity = InstancePublicity.OpenRequest;
        public void ModeratorRequest() => instancePublicity = InstancePublicity.ModeratorRequest;
        public void ClosedRequest() => instancePublicity = InstancePublicity.ClosedRequest;

        public void KCP() => instanceProtocol = InstanceProtocol.KCP;
        public void TCP() => instanceProtocol = InstanceProtocol.TCP;
        public void UDP() => instanceProtocol = InstanceProtocol.UDP;

        public void Create()
        {
            SocketManager.CreateInstance(worldMeta, instancePublicity, instanceProtocol, SelectedGameServer);
            Return();
        }
        public void Return() => gameObject.SetActive(false);

        internal void SetGameServer(GameServer gameServer)
        {
            SelectedGameServer = gameServer;
            GameServersLabel.text = "Selected GameServer: " + gameServer.GameServerId;
        }
        
        private void CreateGameServerTemplate(GameServer gameServer)
        {
            GameObject g = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("GameServer").gameObject;
            GameObject newG = Instantiate(g);
            RectTransform c = newG.GetComponent<RectTransform>();
            newG.GetComponent<GameServerTemplate>().Render(gameServer);
            GameServersDisplay.AddItem(c);
        }

        private void OnGameServersResult(CallbackResult<GameServersResult> result)
        {
            GameServersDisplay.Clear();
            LastGameServers = result.result.GameServers;
            foreach (GameServer gameServer in LastGameServers)
                CreateGameServerTemplate(gameServer);
            if (LastGameServers.Count > 0)
            {
                SetGameServer(LastGameServers.ElementAt(0));
                CreateButton.SetActive(true);
            }
            else
            {
                SelectedGameServer = null;
                GameServersLabel.text = "No GameServers Available! Try again later.. :(";
                CreateButton.SetActive(false);
            }
        }

        private void Update()
        {
            InstancePublicityLabel.text = "Instance Publicity: " + instancePublicity;
            InstanceProtocolLabel.text = "Instance Protocol: " + instanceProtocol;
        }
    }
}