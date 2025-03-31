using System;
using Hypernex.CCK.Unity;
using Hypernex.Player;
using Hypernex.UI.Components;
using Hypernex.UI.Pages;
using UnityEngine;
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

        public Texture2D DefaultProfilePicture;
        public Texture2D DefaultProfileBanner;
        public Texture2D DefaultAvatarBanner;
        public Texture2D DefaultWorldBanner;
        public ConsoleWindow Console;
        public GameObject GameMenuObject;
        public OverlayNotification OverlayNotification;

        public void SignOut()
        {
            APIPlayer.Logout();
            GameMenuObject.SetActive(false);
            UIPage.GetPage<ServerSelectPage>().Show();
        }
        
        public void Exit() => Application.Quit();

        private void Initialize()
        {
            new UnityLogger().SetLogger();
            Console.Initialize();
            GetComponent<DontDestroyMe>().Register();
            UIPage[] uiPages = FindObjectsByType<UIPage>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            Array.Reverse(uiPages);
            foreach (UIPage uiPage in uiPages)
                uiPage.Initialize();
            foreach (UIRender uiRenderer in FindObjectsByType<UIRender>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                uiRenderer.Initialize();
            OverlayNotification.Begin();
        }

        public void OnEnable()
        {
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }
            Instance = this;
            Initialize();
        }


        public void Dispose()
        {
            OverlayNotification.Dispose();
        }
    }
}