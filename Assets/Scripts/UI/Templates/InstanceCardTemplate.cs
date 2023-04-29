using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Player;
using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.UIActions;
using Hypernex.Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class InstanceCardTemplate : MonoBehaviour
    {
        public TMP_Text WorldText;
        public TMP_Text CreatorText;
        public RawImage BannerImage;

        public Texture2D DefaultBanner;

        public Button NavigateButton;

        private LoginPageManager loginPageManager;
        private SafeInstance lastRenderedSafeInstance;
        private WorldMeta lastWorldMeta;
        private User lastHoster;
        private User lastCreator;

        public void Render(LoginPageManager lpm, SafeInstance instance, WorldMeta worldMeta, User hoster = null, User creator = null)
        {
            WorldText.text = worldMeta.Name;
            if(hoster != null)
                CreatorText.text = "Hosted By " + hoster.Username + " (" + instance.InstancePublicity + ")";
            if (creator != null)
                CreatorText.text = "Created By " + creator.Username;
            if (!string.IsNullOrEmpty(worldMeta.ThumbnailURL))
                DownloadTools.DownloadBytes(worldMeta.ThumbnailURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = BannerImage.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            BannerImage.texture = ImageTools.BytesToTexture2D(bytes);
                    });
            else
                BannerImage.texture = DefaultBanner;
            loginPageManager = lpm;
            lastRenderedSafeInstance = instance;
            lastWorldMeta = worldMeta;
            lastHoster = hoster;
            lastCreator = creator;
        }

        private void GetAllInstanceHosts(Action<List<(SafeInstance, User)>> callback, List<SafeInstance> instances, List<(SafeInstance, User)> c = null)
        {
            if (instances.Count <= 0)
            {
                callback.Invoke(new List<(SafeInstance, User)>());
                return;
            }
            List<(SafeInstance, User)> temp;
            if (c == null)
                temp = new List<(SafeInstance, User)>();
            else
                temp = c;
            SafeInstance sharedInstance = instances.ElementAt(0);
            APIPlayer.APIObject.GetUser(result =>
            {
                if (result.success)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        c.Add((sharedInstance, result.result.UserData))));
                instances.Remove(sharedInstance);
                if(instances.Count > 0)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() => GetAllInstanceHosts(callback, instances, temp)));
                else
                    QuickInvoke.InvokeActionOnMainThread(callback, temp);
            }, sharedInstance.InstanceCreatorId);
        }
    
        private void Start() => NavigateButton.onClick.AddListener(() =>
        {
            // TODO: Make and Display Instance Page
            if (lastHoster != null)
            {
                // Direct to an Instance Screen
            }
            else if (lastCreator != null)
            {
                // Direct to a World Page
                GetAllInstanceHosts(instances =>
                {
                    loginPageManager.WorldTemplate.Render(lastWorldMeta, lastCreator, instances);
                }, APIPlayer.SharedInstances);
            }
        });
    }
}