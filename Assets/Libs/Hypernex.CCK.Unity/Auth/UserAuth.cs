using System;
using System.Net.Http;
using System.Threading.Tasks;
using HypernexSharp;
using HypernexSharp.APIObjects;
using SimpleJSON;
using LoginResult = HypernexSharp.APIObjects.LoginResult;

namespace Hypernex.CCK.Unity.Auth
{
    public class UserAuth
    {
        private const string GEOIP = "https://api.seeip.org/geoip";
        
        public static UserAuth Instance { get; set; }

        public static Action OnInvalidConfigLoaded = () => { };
        private static HttpClient httpClient = new HttpClient();

        public bool Needs2FA { get; private set; }
        public bool IsAuth { get; private set; }
        public string Id => user.Id;
        public string Username => user.Username;
        public string APIURL => hypernexObject.Settings.APIURL;
        public float Latitude { get; private set; }
        public float Longitude { get; private set; }
        
        private string domain;
        private HypernexSettings settings;
        internal HypernexObject hypernexObject;

        public User user;
        internal Token token;

        public UserAuth(string domain)
        {
            this.domain = domain;
            SetGeo();
            Instance = this;
        }

        public UserAuth(AuthConfig authConfig)
        {
            domain = authConfig.TargetDomain;
            Login(authConfig.SavedUserId, authConfig.SavedToken);
            SetGeo();
            Instance = this;
        }

        private async void SetGeo()
        {
            string r = await httpClient.GetStringAsync(GEOIP);
            JSONNode n = JSON.Parse(r);
            Latitude = n["latitude"].AsFloat;
            Longitude = n["longitude"].AsFloat;
        }
        
        public async Task<LoginResult> Login(string username, string password, string twofa)
        {
            IsAuth = false;
            settings = new HypernexSettings(username, password, twofa)
            {
                TargetDomain = domain,
                IsHTTP = AuthConfig.LoadedConfig.UseHTTP
            };
            hypernexObject = new HypernexObject(settings);
            TaskCompletionSource<LoginResult> tcs = new TaskCompletionSource<LoginResult>();
            hypernexObject.Login(result =>
            {
                if (!result.success)
                {
                    tcs.SetResult(LoginResult.Incorrect);
                    return;
                }
                if(result.result.Result == LoginResult.Correct)
                {
                    token = result.result.Token;
                    HandleLogin(result.result);
                }
                else if (result.result.Result == LoginResult.Missing2FA)
                    Needs2FA = true;
                tcs.SetResult(result.result.Result);
            });
            return await tcs.Task;
        }
        
        private void Login(string userid, string tokenContent)
        {
            IsAuth = false;
            settings = new HypernexSettings(userid, tokenContent)
            {
                TargetDomain = domain,
                IsHTTP = AuthConfig.LoadedConfig.UseHTTP
            };
            hypernexObject = new HypernexObject(settings);
            TaskCompletionSource<LoginResult> tcs = new TaskCompletionSource<LoginResult>();
            hypernexObject.Login(result =>
            {
                if (!result.success)
                {
                    tcs.SetResult(LoginResult.Incorrect);
                    // Invalidate config
                    SaveToConfig(true);
                    OnInvalidConfigLoaded.Invoke();
                    return;
                }
                if(result.result.Result == LoginResult.Correct)
                {
                    token = result.result.Token;
                    HandleLogin(result.result);
                }
                else if (result.result.Result == LoginResult.Missing2FA)
                    Needs2FA = true;
                tcs.SetResult(result.result.Result);
            });
        }

        private async void HandleLogin(HypernexSharp.API.APIResults.LoginResult loginResult)
        {
            TaskCompletionSource<User> userTask = new TaskCompletionSource<User>();
            hypernexObject.GetUser(loginResult.Token, result =>
            {
                if (!result.success)
                {
                    Logger.CurrentLogger.Error("Could not find User from Login!");
                    userTask.SetResult(null);
                    return;
                }
                userTask.SetResult(result.result.UserData);
            });
            user = await userTask.Task;
            IsAuth = user != null;
            if(!IsAuth || user == null) return;
            SaveToConfig();
        }

        public async Task<bool> Logout()
        {
            if(!IsAuth) return false;
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            hypernexObject.Logout(e =>
            {
                if (!e.success)
                {
                    Logger.CurrentLogger.Error("Could not log out!");
                    tcs.SetResult(false);
                    return;
                }
                user = null;
                token = null;
                IsAuth = false;
                Needs2FA = false;
                SaveToConfig(true);
                tcs.SetResult(true);
            }, user, token);
            return await tcs.Task;
        }
        
        private void SaveToConfig(bool empty = false)
        {
            AuthConfig c = AuthConfig.GetConfig();
            c.TargetDomain = empty ? String.Empty : settings.TargetDomain;
            c.SavedUserId = empty ? String.Empty : user.Id;
            c.SavedToken = empty ? String.Empty : token.content;
            AuthConfig.SaveConfig();
        }
    }
}