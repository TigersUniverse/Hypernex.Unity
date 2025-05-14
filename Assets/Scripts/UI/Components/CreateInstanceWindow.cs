using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using HypernexSharp.SocketObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Components
{
    public class CreateInstanceWindow : MonoBehaviour
    {
        public TMP_Text GameServersLabel;
        public RectTransform GameServersDisplay;
        public ToggleButton[] PublicityButtons;
        public GameObject CreateButton;
        
        private static List<GameServer> LastGameServers = new();
        private GameServer SelectedGameServer;
        private List<ToggleButton> gameServerToggles = new();
        private WorldMeta worldMeta;

        public void SetGameServer(GameServerRender gameServerRender) =>
            SelectedGameServer = gameServerRender.gameServer;
        private void SetGameServer(GameServer gameServer) => SelectedGameServer = gameServer;
        
        private void CreateGameServerTemplate(GameServer gameServer)
        {
            IRender<GameServer> gameServerRender = Defaults.GetRenderer<GameServer>("GameServerTemplate");
            gameServerRender.Render(gameServer);
            GameServersDisplay.AddChild(gameServerRender.transform);
            gameServerToggles.Add(gameServerRender.GetComponent<ToggleButton>());
            ToggleButton[] newArr = gameServerToggles.ToArray();
            foreach (ToggleButton toggleButton in gameServerToggles)
                toggleButton.Family = newArr;
        }
        
        private void OnGameServersResult(CallbackResult<GameServersResult> result)
        {
            LastGameServers.Clear();
            GameServersDisplay.ClearChildren();
            gameServerToggles.Clear();
            LastGameServers = result.result.GameServers;
            GeoTools.SortGameServers(ref LastGameServers);
            foreach (GameServer gameServer in LastGameServers)
                CreateGameServerTemplate(gameServer);
            if (LastGameServers.Count > 0)
            {
                SetGameServer(LastGameServers.ElementAt(0));
                GameServersDisplay.transform.GetChild(0).GetComponent<ToggleButton>().Select();
                GameServersLabel.text = String.Empty;
                CreateButton.SetActive(true);
            }
            else
            {
                SelectedGameServer = null;
                GameServersLabel.text = "No GameServers Available! Try again later.. :(";
                CreateButton.SetActive(false);
            }
        }

        public void Apply(WorldMeta w)
        {
            worldMeta = w;
            LastGameServers.Clear();
            GameServersDisplay.ClearChildren();
            gameServerToggles.Clear();
            APIPlayer.APIObject.GetGameServers(r =>
                QuickInvoke.InvokeActionOnMainThread(new Action(() => OnGameServersResult(r))));
            gameObject.SetActive(true);
        }
        
        public void Create()
        {
            OverlayNotification.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
            {
                Header = "Creating Instance",
                Description = $"Creating instance for World {worldMeta.Name}, Please Wait"
            });
            InstancePublicity instancePublicity = (InstancePublicity) PublicityButtons.GetSelectedIndex();
            SocketManager.CreateInstance(worldMeta, instancePublicity, InstanceProtocol.KCP, SelectedGameServer);
            Return();
        }
        
        public void Return() => gameObject.SetActive(false);
    }
}