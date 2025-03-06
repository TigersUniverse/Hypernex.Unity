using System.Collections.Generic;
using System.Linq;
using Hypernex.Configuration;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using HypernexSharp;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using LoginResult = HypernexSharp.APIObjects.LoginResult;

namespace Hypernex.UI.Pages
{
    public class UserSelectPage : UIPage
    {
        public ConfigUser User { get; private set; }
        
        private SelectedUserRender _selectedUser;
        internal SelectedUserRender SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                string txt = _selectedUser == null ? "Select a Server" : _selectedUser.User.Username;
                UserText.text = txt;
                User = _selectedUser.User;
            }
        }

        public Transform Items;
        public TMP_Text UserText;
        public TMP_InputField Username;
        public TMP_InputField Password;
        public GameObject TwoFAContainer;
        public TMP_InputField TwoFACode;

        private ServerSelectPage serverSelectPage;

        public void OnLogin()
        {
            
        }

        public void LinkButton() => Application.OpenURL(Init.DEFAULT_WEB_URL);

        public void OnInputLogin()
        {
            HypernexSettings settings = new HypernexSettings(Username.text, Password.text, TwoFACode.text)
            {
                TargetDomain = serverSelectPage.Server,
                IsHTTP = Init.Instance.UseHTTP
            };
        }

        public void Cancel2FA() => TwoFAContainer.SetActive(false);
        
        private void HandleSetUser(HypernexSettings settings)
        {
            APIPlayer.Create(settings);
            APIPlayer.Login((lr, u) => HandleSetUser(lr, u));
        }
        
        private void HandleSetUser(HypernexSharp.API.APIResults.LoginResult loginResult, User user, ConfigUser c = null)
        {
            switch (loginResult?.Result ?? LoginResult.Incorrect)
            {
                case LoginResult.Incorrect:
                    // TODO: Message Popup
                    break;
                case LoginResult.Missing2FA:
                    TwoFAContainer.SetActive(true);
                    break;
                case LoginResult.Warned:
                    // TODO: Create Page and Transition
                    break;
                case LoginResult.Banned:
                    // TODO: Create Page and Transition
                    break;
                case LoginResult.Correct:
                    foreach (ConfigUser configUser in new List<ConfigUser>(ConfigManager.LoadedConfig.SavedAccounts))
                    {
                        if (configUser.UserId == user.Id && configUser.Server.ToLower() == serverSelectPage.Server.ToLower())
                        {
                            if (c == null)
                            {
                                configUser.TokenContent = loginResult!.Token.content;
                                c = configUser;
                            }
                        }
                    }
                    if (c == null)
                    {
                        c = new ConfigUser
                        {
                            UserId = user.Id,
                            Username = user.Username,
                            TokenContent = loginResult!.Token.content,
                            Server = serverSelectPage.Server
                        };
                        ConfigManager.LoadedConfig.SavedAccounts.Add(c);
                    }
                    ConfigManager.SelectedConfigUser = c;
                    ConfigManager.SaveConfigToFile();
                    break;
            }
        }
        
        private void Refresh(Config config)
        {
            Items.ClearChildren();
            ConfigUser[] configUsers = config.SavedAccounts.Where(x => x.Server == serverSelectPage.Server).ToArray();
            foreach (ConfigUser savedUser in configUsers)
            {
                IRender<ConfigUser> newUser = Defaults.GetRenderer<ConfigUser>("UserItem");
                newUser.Render(savedUser);
                newUser.transform.SetParent(Items);
            }
        }
        
        internal override void Initialize()
        {
            if(!HasInitialized)
            {
                ConfigManager.OnConfigLoaded += Refresh;
                serverSelectPage = GetPage<ServerSelectPage>();
                UserText.text = "Select an Account";
            }
            else
                Refresh(ConfigManager.LoadedConfig);
            base.Initialize();
        }
    }
}