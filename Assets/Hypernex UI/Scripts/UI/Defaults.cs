using System;
using Hypernex.CCK.Unity;
using Hypernex.Tools;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class Defaults : MonoBehaviour
    {
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

        private void Initialize()
        {
            new UnityLogger().SetLogger();
            GetComponent<DontDestroyMe>().Register();
            UIPage[] uiPages = FindObjectsByType<UIPage>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            Array.Reverse(uiPages);
            foreach (UIPage uiPage in uiPages)
                uiPage.Initialize();
            foreach (UIRender uiRenderer in FindObjectsByType<UIRender>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                uiRenderer.Initialize();
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
    }
}