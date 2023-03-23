using System;
using HypernexSharp;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using Hypernex.Tools;
using UnityEngine;
using Logger = Hypernex.Logging.Logger;
using LoginResult = HypernexSharp.APIObjects.LoginResult;

namespace Hypernex.Player
{
    public class APIPlayer : MonoBehaviour
    {
        private static HypernexSettings APISettings;
        public static HypernexObject APIObject { get; private set; }
    
        public static User APIUser { get; private set; }
        internal static Token CurrentToken;
    
        public static Action<User> OnUser = user => { };
        public static Action<User> OnUserRefresh = user => { };
        public static Action OnLogout = () => { };

        public static void Create(HypernexSettings settings)
        {
            APISettings = settings;
            APIObject = new HypernexObject(settings);
        }

        public static void Login(Action<HypernexSharp.API.APIResults.LoginResult, User> result)
        {
            if (APISettings != null && APIObject != null)
            {
                APIObject.Login(loginResult =>
                {
                    if (loginResult.success && loginResult.result.Result is LoginResult.Correct or LoginResult.Warned)
                    {
                        APIObject.GetUser(loginResult.result.Token, getUserResult =>
                        {
                            if (getUserResult.success)
                            {
                                APIUser = getUserResult.result.UserData;
                                CurrentToken = loginResult.result.Token;
                                QuickInvoke.InvokeActionOnMainThread(result, loginResult.result, getUserResult.result.UserData);
                                if(loginResult.result.Result != LoginResult.Warned)
                                    QuickInvoke.InvokeActionOnMainThread(OnUser, APIUser);
                                Logger.CurrentLogger.Log("Signed-In as " + getUserResult.result.UserData.Username + "!");
                            }
                            else
                                QuickInvoke.InvokeActionOnMainThread(result, loginResult.result, null);
                        });
                    }
                    else
                        QuickInvoke.InvokeActionOnMainThread(result, loginResult.result, null);
                });
            }
            else
                QuickInvoke.InvokeActionOnMainThread(result, null, null);
        }

        public static void Register(Action<bool, SignupResult> result)
        {
            if (APISettings != null && APIObject != null)
            {
                APIObject.CreateUser(signupResult =>
                {
                    if (signupResult.success)
                    {
                        APIUser = signupResult.result.UserData;
                        CurrentToken = signupResult.result.UserData.AccountTokens[0];
                        QuickInvoke.InvokeActionOnMainThread(OnUser, APIUser);
                        QuickInvoke.InvokeActionOnMainThread(result, true, signupResult.result);
                    }
                    else
                        QuickInvoke.InvokeActionOnMainThread(result, false, null);
                });
            }
            else
                QuickInvoke.InvokeActionOnMainThread(result, false, null);
        }

        public static void RefreshUser()
        {
            if (APISettings != null && APIObject != null)
                APIObject.GetUser(CurrentToken, getUserResult =>
                {
                    if (!getUserResult.success) return;
                    APIUser = getUserResult.result.UserData;
                    QuickInvoke.InvokeActionOnMainThread(OnUserRefresh, getUserResult.result.UserData);
                });
        }

        public static void Logout(Action<bool> result = null)
        {
            if (APISettings != null && APIObject != null)
                APIObject.Logout(r =>
                {
                    if (r.success)
                    {
                        APIUser = null;
                        CurrentToken = null;
                        QuickInvoke.InvokeActionOnMainThread(OnLogout);
                    }
                    if(result != null)
                        QuickInvoke.InvokeActionOnMainThread(result, r.success);
                }, APIUser, CurrentToken);
            else
            {
                if(result != null)
                    QuickInvoke.InvokeActionOnMainThread(result, false);
            }
        }
    }
}
