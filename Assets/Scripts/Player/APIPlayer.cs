using System;
using System.Collections.Generic;
using HypernexSharp;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using Hypernex.Tools;
using Hypernex.UIActions;
using Hypernex.UIActions.Data;
using HypernexSharp.Socketing;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;
using LoginResult = HypernexSharp.APIObjects.LoginResult;

namespace Hypernex.Player
{
    public class APIPlayer : MonoBehaviour
    {
        private static HypernexSettings APISettings;
        public static HypernexObject APIObject { get; private set; }
    
        public static User APIUser { get; private set; }
        internal static Token CurrentToken;
        internal static UserSocket UserSocket { get; private set; }

        private static List<SafeInstance> _sharedInstances = new();
        public static List<SafeInstance> SharedInstances => new(_sharedInstances);

        public static bool IsFullReady =>
            APIObject != null && APIUser != null && CurrentToken != null && (UserSocket?.IsOpen ?? false);
    
        public static Action<User> OnUser = user => { };
        public static Action OnSocketConnect = () => { };
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
                                UserSocket = APIObject.OpenUserSocket(APIUser, CurrentToken, () =>
                                    QuickInvoke.InvokeActionOnMainThread(new Action(SocketManager.InitSocket)), false);
                                QuickInvoke.InvokeActionOnMainThread(OnUserRefresh, APIUser);
                                OverlayManager.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
                                {
                                    Header = "Signed-In!",
                                    Description = "Signed-In as " + getUserResult.result.UserData.Username + "!"
                                });
                            }
                            else
                                QuickInvoke.InvokeActionOnMainThreadObject(result, new object[]{loginResult.result, null});
                        });
                    }
                    else
                        QuickInvoke.InvokeActionOnMainThreadObject(result, new object[]{loginResult.result, null});
                });
            }
            else
                QuickInvoke.InvokeActionOnMainThreadObject(result, new object[]{null, null});
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
                        QuickInvoke.InvokeActionOnMainThreadObject(result, new object[]{false, null});
                });
            }
            else
                QuickInvoke.InvokeActionOnMainThreadObject(result, new object[]{false, null});
        }

        public static void RefreshUser(Action<User> additionalOnRefresh = null)
        {
            if (APISettings != null && APIObject != null)
                APIObject.GetUser(CurrentToken, getUserResult =>
                {
                    if (!getUserResult.success) return;
                    APIUser = getUserResult.result.UserData;
                    QuickInvoke.InvokeActionOnMainThread(OnUserRefresh, getUserResult.result.UserData);
                    if(additionalOnRefresh != null)
                        QuickInvoke.InvokeActionOnMainThread(additionalOnRefresh, getUserResult.result.UserData);
                });
        }

        public static void GetAllSharedInstances(Action<List<SafeInstance>> Instances)
        {
            if (!IsFullReady)
            {
                Instances.Invoke(new List<SafeInstance>());
                return;
            }
            APIObject.GetInstances(result =>
            {
                if (result.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(Instances, result.result.SafeInstances);
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        _sharedInstances = new List<SafeInstance>(result.result.SafeInstances)));
                }
                else
                {
                    QuickInvoke.InvokeActionOnMainThread(Instances, new List<SafeInstance>());
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        _sharedInstances = new List<SafeInstance>()));
                }
            }, APIUser, CurrentToken);
        }

        public static void Logout(Action<bool> result = null)
        {
            if (APISettings != null && APIObject != null)
            {
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
                APIObject.CloseUserSocket();
            }
            else
            {
                if(result != null)
                    QuickInvoke.InvokeActionOnMainThread(result, false);
            }
        }
    }
}
