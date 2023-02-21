using System;
using System.Globalization;
using HypernexSharp;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using LoginResult = HypernexSharp.APIObjects.LoginResult;

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

    public void Start()
    {
        JoinSavedButton.onClick.AddListener(() =>
        {
            if(ServerDropdown.options.Count > 0)
                SetServer(ServerDropdown.options[ServerDropdown.value].text);
        });
        RemoveSavedButton.onClick.AddListener(() =>
        {
            if (ServerDropdown.options.Count > 0)
            {
                ConfigManager.LoadedConfig.SavedServers.Remove(ServerDropdown.options[ServerDropdown.value].text);
                ConfigManager.SaveConfigToFile();
            }
        });
        JoinInputButton.onClick.AddListener(() => SetServer(ServerInput.text));
        JoinAndSaveInputButton.onClick.AddListener(() =>
        {
            SetServer(ServerInput.text);
            ConfigManager.LoadedConfig.SavedServers.Add(ServerInput.text);
            ConfigManager.SaveConfigToFile();
        });
        UserContinueButton.onClick.AddListener(() =>
        {
            if (UserDropdown.options.Count > 0)
                SetUser(ConfigManager.LoadedConfig.SavedAccounts[UserDropdown.value]);
        });
        RemoveSelectedUserButton.onClick.AddListener(() =>
        {
            if (UserDropdown.options.Count > 0)
            {
                ConfigManager.LoadedConfig.SavedAccounts.Remove(
                    ConfigManager.LoadedConfig.SavedAccounts[UserDropdown.value]);
                ConfigManager.SaveConfigToFile();
            }
        });
        SignInButton.onClick.AddListener(() => SetUser(SignInUsernameInput.text, SignInPasswordInput.text));
        SignInAndSaveButton.onClick.AddListener(() =>
            SetUser(SignInUsernameInput.text, SignInPasswordInput.text, true));
        SignUpInsteadButton.onClick.AddListener(() =>
        {
            SignInObject.SetActive(false);
            SignUpObject.SetActive(true);
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
            ServerSelectorObject.SetActive(true);
        };
    }

    private void SetServer(string url)
    {
        UserDropdown.ClearOptions();
        foreach (ConfigUser configUser in ConfigManager.LoadedConfig.SavedAccounts)
        {
            if (configUser.Server.Equals(url))
                UserDropdown.options.Add(new TMP_Dropdown.OptionData(configUser.Username));
        }
        new HypernexObject(new HypernexSettings {TargetDomain = "https://" + url}).IsInviteCodeRequired(result =>
            SignUpInviteCodeInput.gameObject.SetActive(result.result?.inviteCodeRequired ?? false));
        currentURL = url;
        ServerSelectorObject.SetActive(false);
        UserSelectorGameObject.SetActive(true);
    }

    private bool isSettingUser;

    private void SetUser(ConfigUser configUser)
    {
        if (!isSettingUser)
        {
            HypernexSettings settings = new HypernexSettings(configUser.UserId, configUser.TokenContent)
                {TargetDomain = "https://" + currentURL};
            APIPlayer.Create(settings);
            APIPlayer.Login(HandleSetUser);
        }
    }

    private void SetUser(string username, string password, bool save = false)
    {
        saveLogin = save;
        if (!isSettingUser)
        {
            isSettingUser = true;
            HypernexSettings settings = new HypernexSettings(username, password) {TargetDomain = "https://" + currentURL};
            APIPlayer.Create(settings);
            APIPlayer.Login(HandleSetUser);
        }
    }
    
    private void SetUser(string username, string password, string twofa)
    {
        if (!isSettingUser)
        {
            isSettingUser = true;
            HypernexSettings settings = new HypernexSettings(username, password, twofacode: twofa)
                {TargetDomain = "https://" + currentURL};
            APIPlayer.Create(settings);
            APIPlayer.Login(HandleSetUser);
        }
    }

    private void SetNewUser(string username, string email, string password, string inviteCode = "")
    {
        saveLogin = true;
        if (!isSettingUser)
        {
            isSettingUser = true;
            HypernexSettings settings = new HypernexSettings(username, email, password, inviteCode)
                {TargetDomain = "https://" + currentURL};
            APIPlayer.Create(settings);
            APIPlayer.Register(HandleNewSetUser);
        }
    }

    private void HandleSetUser(HypernexSharp.API.APIResults.LoginResult loginResult, User user)
    {
        switch (loginResult?.Result ?? LoginResult.Incorrect)
        {
            case LoginResult.Incorrect:
                Logger.CurrentLogger.Error("Incorrect Credentials!");
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
                    ConfigUser c = new ConfigUser
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        TokenContent = loginResult!.Token.content,
                        Server = currentURL
                    };
                    ConfigManager.LoadedConfig.SavedAccounts.Add(c);
                    ConfigManager.SaveConfigToFile();
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
        ConfigManager.LoadedConfig.SavedAccounts.Add(c);
        ConfigManager.SaveConfigToFile();
    }

    private void OnConfigLoaded(Config config)
    {
        ServerDropdown.ClearOptions();
        foreach (string savedServer in config.SavedServers)
            ServerDropdown.options.Add(new TMP_Dropdown.OptionData(savedServer));
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
