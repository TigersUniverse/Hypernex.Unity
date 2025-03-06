using Hypernex.Configuration;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Pages
{
    public class ServerSelectPage : UIPage
    {
        public string Server { get; private set; }
        
        private SelectedServerRender _selectedServer;
        internal SelectedServerRender SelectedServer
        {
            get => _selectedServer;
            set
            {
                _selectedServer = value;
                string txt = _selectedServer == null ? "Select a Server" : _selectedServer.Server;
                ServerText.text = txt;
            }
        }
        
        public Transform Items;
        public TMP_Text ServerText;
        public TMP_InputField InputServer;

        public void OnConnect()
        {
            if(SelectedServer == null) return;
            HandleConnect(SelectedServer.Server);
        }

        public void LinkButton() => InputServer.text = Init.DEFAULT_DOMAIN;

        public void OnInputConnect()
        {
            string s = InputServer.text;
            if(string.IsNullOrEmpty(s)) return;
            HandleConnect(s);
        }

        private void HandleConnect(string s)
        {
            Server = s;
            UserSelectPage userSelectPage = GetPage<UserSelectPage>();
            userSelectPage.Show();
        }
        
        private void Refresh(Config config)
        {
            Items.ClearChildren();
            foreach (string savedServer in config.SavedServers)
            {
                IRender<string> newServer = Defaults.GetRenderer<string>("ServerItem");
                newServer.Render(savedServer);
                newServer.transform.SetParent(Items);
            }
        }

        internal override void Initialize()
        {
            if(!HasInitialized)
            {
                ConfigManager.OnConfigLoaded += Refresh;
                ServerText.text = "Select a Server";
            }
            else
                Refresh(ConfigManager.LoadedConfig);
            base.Initialize();
        }
    }
}