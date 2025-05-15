using System;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using Hypernex.UI.Components;
using Hypernex.UI.Pages;
using HypernexSharp.APIObjects;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class Defaults : MonoBehaviour, IDisposable
    {
        public const int MAX_RESULTS = 9*2;
        
        public static Defaults Instance { get; private set; }

        public static IRender<T> GetRenderer<T>(string templateName)
        {
            GameObject templateItem = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform.Find(templateName).gameObject;
            GameObject newItem = Instantiate(templateItem);
            UIRender uiRender = newItem.GetComponent<UIRender>();
            uiRender.Initialize();
            return (IRender<T>) uiRender;
        }

        public UIPage[] PagesToInitialize;
        public Texture2D DefaultProfilePicture;
        public Texture2D DefaultProfileBanner;
        public Texture2D DefaultAvatarBanner;
        public Texture2D DefaultWorldBanner;
        public Sprite PublicIcon;
        public Sprite FriendsIcon;
        public Sprite LockedIcon;
        public ConsoleWindow Console;
        public GameObject GameMenuObject;
        public OverlayNotification OverlayNotification;
        public MessagesPage MessagesPage;
        public Image VRDesktopIcon;
        public Sprite VRIcon;
        public Sprite DesktopIcon;

        public void SignOut()
        {
            APIPlayer.Logout();
            GameMenuObject.SetActive(false);
            UIPage.GetPage<ServerSelectPage>().Show();
        }
        
        public void Exit() => Application.Quit();

        public void Respawn()
        {
            if(LocalPlayer.Instance == null) return;
            LocalPlayer.Instance.Respawn();
        }

        public void ToggleMicrophone() =>
            LocalPlayer.Instance.MicrophoneEnabled = !LocalPlayer.Instance.MicrophoneEnabled;

        public void SpawnCamera() => HandleCamera.Create();

        private void Initialize()
        {
            Console.Initialize();
            GetComponent<DontDestroyMe>().Register();
            foreach (UIPage uiPage in PagesToInitialize)
                uiPage.Initialize();
            foreach (UIRender uiRenderer in FindObjectsByType<UIRender>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                uiRenderer.Initialize();
        }

        private void Start()
        {
            SocketManager.OnInvite += invite =>
            {
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    WorldRender.GetWorldMeta(invite.worldId, meta =>
                    {
                        if(meta == null) return;
                        APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        {
                            if (!result.success)
                                return;
                            MessagesPage.PushInvite(invite, meta, result.result.UserData);
                        })), invite.fromUserId, isUserId: true);
                    });
                }));
            };
            SocketManager.OnInviteRequest += inviteRequest =>
            {
                // Don't handle if we aren't in an instance, or no Player is present (which shouldn't be possible)
                if(GameInstance.FocusedInstance == null || APIPlayer.APIUser == null) return;
                // We can't send an invite if the world is Owner only and we're not the owner
                if (GameInstance.FocusedInstance.worldMeta.Publicity == WorldPublicity.OwnerOnly &&
                    GameInstance.FocusedInstance.worldMeta.OwnerId != APIPlayer.APIUser.Id) return;
                APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    if (!result.success)
                        return;
                    MessagesPage.PushInviteRequest(result.result.UserData);
                })), inviteRequest.fromUserId, isUserId: true);
            };
        }

        private void OnEnable()
        {
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }
            Instance = this;
            Initialize();
        }

        private void Update() => VRDesktopIcon.sprite = LocalPlayer.IsVR ? DesktopIcon : VRIcon;

        public void Dispose()
        {
            OverlayNotification.Dispose();
        }
    }
}