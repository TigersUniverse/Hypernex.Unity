using System;
using System.Collections.Generic;
using Hypernex.Configuration;
using Hypernex.UI.Pages;
using TMPro;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction
{
    public class SelectedServerRender : UIRender, IRender<string>, IDisposable
    {
        private static List<SelectedServerRender> selectedServerRenders = new List<SelectedServerRender>();
        
        public string Server { get; private set; }
        
        public TMP_Text ServerText;
        public Toggle Toggle;
        public Button DeleteButton;
        
        private ServerSelectPage serverSelectPage;
        
        public void Render(string t)
        {
            DeleteButton.onClick.RemoveAllListeners();
            DeleteButton.onClick.AddListener(() =>
            {
                ConfigManager.LoadedConfig.SavedServers.Remove(t);
                ConfigManager.SaveConfigToFile();
                Dispose();
            });
            Toggle.onValueChanged.RemoveAllListeners();
            Toggle.onValueChanged.AddListener(v =>
            {
                if(!v) return;
                serverSelectPage.SelectedServer = this;
            });
            ServerText.text = t;
            Server = t;
            if (serverSelectPage != null && !string.IsNullOrEmpty(serverSelectPage.Server))
                Toggle.isOn = serverSelectPage.Server == t;
            else
                Toggle.isOn = false;
        }

        internal override void Initialize()
        {
            if(HasInitialized) return;
            selectedServerRenders.Add(this);
            serverSelectPage = UIPage.GetPage<ServerSelectPage>();
            base.Initialize();
        }

        private void Update()
        {
            if(serverSelectPage == null) return;
            if (serverSelectPage.SelectedServer == null || serverSelectPage.SelectedServer != this)
                Toggle.isOn = false;
        }

        public void Dispose()
        {
            selectedServerRenders.Remove(this);
            Destroy(gameObject);
        }
    }
}