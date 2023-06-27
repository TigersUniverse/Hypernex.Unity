using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Player;
using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.UIActions;
using Hypernex.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class WorldCardTemplate : MonoBehaviour
    {
        public TMP_Text WorldText;
        public TMP_Text CreatorText;
        public RawImage BannerImage;

        public Texture2D DefaultBanner;

        public Button NavigateButton;

        private WorldTemplate worldTemplate;
        private WorldMeta lastWorldMeta;
        private User lastHoster;
        private User lastCreator;
        private LoginPageTopBarButton PreviousPage;

        public void Render(WorldTemplate wt, LoginPageTopBarButton pp, WorldMeta worldMeta, User creator)
        {
            WorldText.text = worldMeta.Name;
            CreatorText.text = "Created By " + creator.Username;
            if (!string.IsNullOrEmpty(worldMeta.ThumbnailURL))
                DownloadTools.DownloadBytes(worldMeta.ThumbnailURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = BannerImage.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            BannerImage.texture = ImageTools.BytesToTexture2D(bytes);
                    });
            else
                BannerImage.texture = DefaultBanner;
            worldTemplate = wt;
            PreviousPage = pp;
            lastWorldMeta = worldMeta;
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
                        temp.Add((sharedInstance, result.result.UserData))));
                instances.Remove(sharedInstance);
                if(instances.Count > 0)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() => GetAllInstanceHosts(callback, instances, temp)));
                else
                    QuickInvoke.InvokeActionOnMainThread(callback, temp);
            }, sharedInstance.InstanceCreatorId);
        }
    
        private void Start() => NavigateButton.onClick.AddListener(() =>
        {
            GetAllInstanceHosts(instances =>
            {
                worldTemplate.Render(lastWorldMeta, lastCreator, instances, PreviousPage);
            }, APIPlayer.SharedInstances);
        });
    }
}