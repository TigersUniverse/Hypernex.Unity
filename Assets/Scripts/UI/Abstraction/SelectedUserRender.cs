using System;
using System.Collections.Generic;
using Hypernex.Configuration;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.UI.Pages;
using TMPro;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction
{
    public class SelectedUserRender : UIRender, IRender<ConfigUser>, IDisposable
    {
        private static List<SelectedUserRender> selectedUserRenders = new List<SelectedUserRender>();
        
        public ConfigUser User { get; private set; }
        
        public TMP_Text UsernameText;
        public Toggle Toggle;
        public Button DeleteButton;
        
        private ServerSelectPage serverSelectPage;
        private UserSelectPage userSelectPage;
        
        public void Render(ConfigUser t)
        {
            DeleteButton.onClick.RemoveAllListeners();
            DeleteButton.onClick.AddListener(() =>
            {
                int index = ConfigManager.LoadedConfig.SavedAccounts.FindIndex(x =>
                    x.Server == serverSelectPage.Server && x.UserId == t.UserId);
                ConfigManager.LoadedConfig.SavedAccounts.RemoveAt(index);
                ConfigManager.SaveConfigToFile();
                Dispose();
            });
            Toggle.onValueChanged.RemoveAllListeners();
            Toggle.onValueChanged.AddListener(v =>
            {
                if(!v) return;
                userSelectPage.SelectedUser = this;
            });
            UsernameText.text = t.Username;
            User = t;
            if (serverSelectPage != null && !string.IsNullOrEmpty(serverSelectPage.Server))
            {
                if (userSelectPage != null && userSelectPage.User != null)
                    Toggle.isOn = serverSelectPage.Server == User.Server && userSelectPage.User.UserId == User.UserId;
                else
                    Toggle.isOn = false;
            }
            else
                Toggle.isOn = false;
        }

        internal override void Initialize()
        {
            if(HasInitialized) return;
            selectedUserRenders.Add(this);
            serverSelectPage = UIPage.GetPage<ServerSelectPage>();
            userSelectPage = UIPage.GetPage<UserSelectPage>();
            base.Initialize();
        }

        private void Update()
        {
            if(userSelectPage == null) return;
            if (userSelectPage.SelectedUser == null || userSelectPage.SelectedUser != this)
                Toggle.isOn = false;
        }

        public void Dispose()
        {
            selectedUserRenders.Remove(this);
            Destroy(gameObject);
        }
    }
}