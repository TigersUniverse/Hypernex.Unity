using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.Configuration;
using Hypernex.Player;
using HypernexSharp;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.Tools;
using Hypernex.UIActions.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;
using LoginResult = HypernexSharp.APIObjects.LoginResult;

namespace Hypernex.UIActions
{
    public class PreLoginManager : MonoBehaviour
    {
        public GameObject ServerSelectorObject;
        public TMP_Dropdown ServerDropdown;
        public TMP_InputField ServerInput;
        public Button JoinSavedButton;
        public Button RemoveSavedButton;
        public Button JoinInputButton;
        public Button JoinAndSaveInputButton;

        public GameObject UserSelectorGameObject;
        public TMP_Dropdown UserDropdown;
        public Button UserContinueButton;
        public Button RemoveSelectedUserButton;
        public Button ReturnUserSelectorButton;

        public GameObject SignInObject;
        public TMP_InputField SignInUsernameInput;
        public TMP_InputField SignInPasswordInput;
        public Button SignInButton;
        public Button SignInAndSaveButton;
        public Button SignUpInsteadButton;

        public GameObject SignUpObject;
        public TMP_InputField SignUpUsernameInput;
        public TMP_InputField SignUpEmailInput;
        public TMP_InputField SignUpPasswordInput;
        public TMP_InputField SignUpInviteCodeInput;
        public Button SignUpButton;
        public Button SignInInsteadButton;

        public GameObject TwoFAPanel;
        public TMP_InputField TwoFAInput;
        public Button TwoFASubmitButton;
        public Button TwoFACancelButton;

        public GameObject BanWarnNoteScreen;
        public TMP_Text StatusTitle;
        public TMP_Text BeginDate;
        public TMP_Text EndDate;
        public TMP_Text Reason;
        public TMP_Text Description;
        public Button UnderstandButton;
        public Button ExitButton;

        public GameObject LoginPageObject;

        private string currentURL;
        private bool saveLogin;
        private List<int> userIndices = new List<int>();

        public void Start()
        {
            ServerDropdown.ClearOptions();
            JoinSavedButton.onClick.AddListener(() =>
            {
                if(ServerDropdown.options.Count > 0)
                    SetServer(ServerDropdown.options[ServerDropdown.value].text);
            });
            RemoveSavedButton.onClick.AddListener(() =>
            {
                if (ServerDropdown.options.Count > 0)
                {
                    ConfigManager.LoadedConfig.SavedServers.Remove(ServerDropdown.options[ServerDropdown.value].text
                        .ToLower());
                    ConfigManager.SaveConfigToFile();
                    RefreshServers(ConfigManager.LoadedConfig);
                }
            });
            JoinInputButton.onClick.AddListener(() => SetServer(ServerInput.text));
            JoinAndSaveInputButton.onClick.AddListener(() =>
            {
                SetServer(ServerInput.text);
                if (!ConfigManager.LoadedConfig.SavedServers.Contains(ServerInput.text.ToLower()))
                {
                    ConfigManager.LoadedConfig.SavedServers.Add(ServerInput.text.ToLower());
                    ConfigManager.SaveConfigToFile();
                    RefreshServers(ConfigManager.LoadedConfig);
                }
            });
            UserContinueButton.onClick.AddListener(() =>
            {
                if (UserDropdown.options.Count > 0)
                    SetUser(ConfigManager.LoadedConfig.SavedAccounts[userIndices[UserDropdown.value]]);
            });
            RemoveSelectedUserButton.onClick.AddListener(() =>
            {
                if (UserDropdown.options.Count > 0)
                {
                    ConfigManager.LoadedConfig.SavedAccounts.RemoveAt(userIndices[UserDropdown.value]);
                    ConfigManager.SaveConfigToFile();
                    RefreshUsers(ConfigManager.LoadedConfig);
                }
            });
            SignInButton.onClick.AddListener(() => SetUser(SignInUsernameInput.text, SignInPasswordInput.text));
            SignInAndSaveButton.onClick.AddListener(() => SetUser(SignInUsernameInput.text, SignInPasswordInput.text, true));
            SignUpInsteadButton.onClick.AddListener(() =>
            {
                SignInObject.SetActive(false);
                SignUpObject.SetActive(true);
            });
            ReturnUserSelectorButton.onClick.AddListener(() =>
            {
                UserSelectorGameObject.SetActive(false);
                ServerSelectorObject.SetActive(true);
            });
            SignUpButton.onClick.AddListener(() => SetNewUser(SignUpUsernameInput.text, SignUpEmailInput.text,
                SignUpPasswordInput.text, SignUpInviteCodeInput.text));
            SignInInsteadButton.onClick.AddListener(() =>
            {
                SignUpObject.SetActive(false);
                SignInObject.SetActive(true);
            });
            TwoFACancelButton.onClick.AddListener(() =>
            {
                TwoFAPanel.SetActive(false);
                saveLogin = false;
            });
            ExitButton.onClick.AddListener(Application.Quit);
            ConfigManager.OnConfigLoaded += OnConfigLoaded;
            APIPlayer.OnUser += user =>
            {
                UserSelectorGameObject.SetActive(false);
                ServerSelectorObject.SetActive(false);
                LoginPageObject.SetActive(true);
            };
            APIPlayer.OnLogout += () =>
            {
                UserSelectorGameObject.SetActive(false);
                LoginPageObject.SetActive(false);
                ServerSelectorObject.SetActive(true);
            };
        }

        private void RefreshServers(Config config)
        {
            ServerDropdown.ClearOptions();
            foreach (string savedServer in config.SavedServers)
                ServerDropdown.options.Add(new TMP_Dropdown.OptionData(savedServer));
            ServerDropdown.RefreshShownValue();
        }

        private void RefreshUsers(Config config)
        {
            userIndices.Clear();
            UserDropdown.ClearOptions();
            int i = 0;
            foreach (ConfigUser configUser in config.SavedAccounts)
            {
                if (configUser.Server.Equals(currentURL))
                {
                    userIndices.Add(i);
                    UserDropdown.options.Add(new TMP_Dropdown.OptionData(configUser.Username));
                }
                i++;
            }
            UserDropdown.RefreshShownValue();
        }

        private void SetServer(string url)
        {
            currentURL = url;
            RefreshUsers(ConfigManager.LoadedConfig);
            new HypernexObject(new HypernexSettings {TargetDomain = url, IsHTTP = Init.Instance.UseHTTP})
                .IsInviteCodeRequired(result =>
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        SignUpInviteCodeInput.gameObject.SetActive(result.result.inviteCodeRequired))));
            ServerSelectorObject.SetActive(false);
            UserSelectorGameObject.SetActive(true);
        }

        private bool isSettingUser;

        private void SetUser(ConfigUser configUser)
        {
            if (!isSettingUser)
            {
                isSettingUser = true;
                HypernexSettings settings = new HypernexSettings(configUser.UserId, configUser.TokenContent)
                    {TargetDomain = currentURL, IsHTTP = Init.Instance.UseHTTP};
                APIPlayer.Create(settings);
                APIPlayer.Login((lr, u) => HandleSetUser(lr, u, configUser));
            }
        }

        private void SetUser(string username, string password, bool save = false)
        {
            saveLogin = save;
            if (!isSettingUser)
            {
                isSettingUser = true;
                HypernexSettings settings = new HypernexSettings(username: username, password: password)
                    {TargetDomain = currentURL, IsHTTP = Init.Instance.UseHTTP};
                APIPlayer.Create(settings);
                APIPlayer.Login((lr, u) => HandleSetUser(lr, u));
            }
        }
    
        private void SetUser(string username, string password, string twofa)
        {
            if (!isSettingUser)
            {
                isSettingUser = true;
                HypernexSettings settings = new HypernexSettings(username, password, twofacode: twofa)
                    {TargetDomain = currentURL, IsHTTP = Init.Instance.UseHTTP};
                APIPlayer.Create(settings);
                APIPlayer.Login((lr, u) => HandleSetUser(lr, u));
            }
        }

        private void SetNewUser(string username, string email, string password, string inviteCode = "")
        {
            saveLogin = true;
            if (!isSettingUser)
            {
                isSettingUser = true;
                HypernexSettings settings = new HypernexSettings(username, email, password, inviteCode)
                    {TargetDomain = currentURL, IsHTTP = Init.Instance.UseHTTP};
                APIPlayer.Create(settings);
                APIPlayer.Register(HandleNewSetUser);
            }
        }

        private void HandleSetUser(HypernexSharp.API.APIResults.LoginResult loginResult, User user, ConfigUser c = null)
        {
            switch (loginResult?.Result ?? LoginResult.Incorrect)
            {
                case LoginResult.Incorrect:
                    OverlayManager.AddMessageToQueue(new MessageMeta(MessageUrgency.Error, MessageButtons.None)
                    {
                        Header = "Incorrect Credentials!",
                        Description = "Please try again!"
                    });
                    break;
                case LoginResult.Missing2FA:
                    TwoFAInput.text = String.Empty;
                    TwoFASubmitButton.onClick.RemoveAllListeners();
                    // ConfigUser should NOT reach this point, they have a token
                    TwoFASubmitButton.onClick.AddListener(() =>
                        SetUser(SignInUsernameInput.text, SignInPasswordInput.text, TwoFAInput.text));
                    TwoFAPanel.SetActive(true);
                    break;
                case LoginResult.Warned:
                    ShowWarning(loginResult!.WarnStatus, () =>
                    {
                        BanWarnNoteScreen.SetActive(false);
                        UnderstandButton.onClick.RemoveAllListeners();
                        APIPlayer.OnUser.Invoke(user);
                    });
                    break;
                case LoginResult.Banned:
                    ShowBan(loginResult!.BanStatus);
                    break;
                case LoginResult.Correct:
                    if (saveLogin)
                    {
                        foreach (ConfigUser configUser in new List<ConfigUser>(ConfigManager.LoadedConfig.SavedAccounts))
                        {
                            if (configUser.UserId == user.Id && configUser.Server.ToLower() == currentURL.ToLower())
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
                                Server = currentURL
                            };
                            ConfigManager.LoadedConfig.SavedAccounts.Add(c);
                        }
                        ConfigManager.SelectedConfigUser = c;
                        ConfigManager.SaveConfigToFile();
                    }
                    else
                    {
                        foreach (ConfigUser configUser in new List<ConfigUser>(ConfigManager.LoadedConfig.SavedAccounts))
                        {
                            if (configUser.UserId == user.Id && configUser.Server.ToLower() == currentURL.ToLower())
                            {
                                if (c == null)
                                {
                                    configUser.TokenContent = loginResult!.Token.content;
                                    c = configUser;
                                }
                            }
                        }
                        if (c == null)
                            ConfigManager.SelectedConfigUser = new ConfigUser
                            {
                                UserId = user.Id,
                                Username = user.Username,
                                TokenContent = loginResult!.Token.content,
                                Server = currentURL
                            };
                        else
                            ConfigManager.SelectedConfigUser = c;
                    }
                    break;
            }
            isSettingUser = false;
        }

        private void HandleNewSetUser(bool r, SignupResult signupResult)
        {
            ConfigUser c = new ConfigUser
            {
                UserId = signupResult.UserData.Id,
                Username = signupResult.UserData.Username,
                TokenContent = signupResult.UserData.AccountTokens[0].content,
                Server = currentURL
            };
            ConfigManager.SelectedConfigUser = c;
            ConfigManager.LoadedConfig.SavedAccounts.Add(c);
            ConfigManager.SaveConfigToFile();
        }

        private void OnConfigLoaded(Config config)
        {
            RefreshServers(config);
            RefreshUsers(config);
        }

        private void ShowWarning(WarnStatus warnStatus, UnityAction onUnderstand)
        {
            StatusTitle.text = "You have been Warned";
            BeginDate.text = "Date Warned: " + DateTools.UnixTimeStampToDateTime(warnStatus.TimeWarned).ToLocalTime()
                .ToString(CultureInfo.InvariantCulture);
            EndDate.text = String.Empty;
            Reason.text = warnStatus.WarnReason;
            Description.text = warnStatus.WarnDescription;
            ExitButton.gameObject.SetActive(false);
            UnderstandButton.onClick.RemoveAllListeners();
            UnderstandButton.onClick.AddListener(onUnderstand);
            UnderstandButton.gameObject.SetActive(true);
            UserSelectorGameObject.SetActive(false);
            BanWarnNoteScreen.SetActive(true);
        }

        private void ShowBan(BanStatus banStatus)
        {
            StatusTitle.text = "You have been Banned";
            BeginDate.text = "Date Banned: " + DateTools.UnixTimeStampToDateTime(banStatus.BanBegin).ToLocalTime()
                .ToString(CultureInfo.InvariantCulture);
            EndDate.text = "Length of Ban: " + DateTools.UnixTimeStampToDateTime(banStatus.BanEnd).ToLocalTime()
                .ToString(CultureInfo.InvariantCulture);
            Reason.text = banStatus.BanReason;
            Description.text = banStatus.BanDescription;
            UnderstandButton.gameObject.SetActive(false);
            ExitButton.gameObject.SetActive(true);
            UserSelectorGameObject.SetActive(false);
            BanWarnNoteScreen.SetActive(true);
        }
    }
}
