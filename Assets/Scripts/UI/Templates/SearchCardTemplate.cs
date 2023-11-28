using System;
using System.Collections.Generic;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UIActions;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class SearchCardTemplate : MonoBehaviour
    {
        public LoginPageTopBarButton SearchTopBarButton;
        
        public RawImage Banner;
        public TMP_Text Name;
        public TMP_Text Extra;
        public Button ClickButton;
        public Texture2D DefaultBanner;
        
        public ProfileTemplate ProfileTemplate;
        public AvatarTemplate AvatarTemplate;
        public WorldTemplate WorldTemplate;
        
        private GifRenderer gifRenderer;
        private List<(SafeInstance, User)> lastSafeInstances = new();

        public void Render(User user)
        {
            ClearLastCard();
            if (!string.IsNullOrEmpty(user.Bio.BannerURL))
                DownloadTools.DownloadBytes(user.Bio.BannerURL, bytes =>
                {
                    if (GifRenderer.IsGif(bytes))
                    {
                        gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                        gifRenderer.LoadGif(bytes);
                    }
                    else
                        Banner.texture = ImageTools.BytesToTexture2D(user.Bio.BannerURL, bytes);
                });
            else
                Banner.texture = DefaultBanner;
            Name.text = string.IsNullOrEmpty(user.Bio.DisplayName)
                ? user.Username
                : user.Bio.DisplayName + " (@" + user.Username + ")";
            if (user.Bio.Status == Status.Offline || string.IsNullOrEmpty(user.Bio.StatusText))
                Extra.text = user.Bio.Status.ToString();
            else
                Extra.text = GetTextFromStatus(user.Bio.Status);
            ClickButton.onClick.AddListener(() => ProfileTemplate.Render(user));
        }

        public void Render(AvatarMeta avatarMeta, User creator)
        {
            ClearLastCard();
            if (!string.IsNullOrEmpty(avatarMeta.ImageURL))
                DownloadTools.DownloadBytes(avatarMeta.ImageURL, bytes =>
                {
                    if (GifRenderer.IsGif(bytes))
                    {
                        gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                        gifRenderer.LoadGif(bytes);
                    }
                    else
                        Banner.texture = ImageTools.BytesToTexture2D(avatarMeta.ImageURL, bytes);
                });
            else
                Banner.texture = DefaultBanner;
            Name.text = avatarMeta.Name;
            Extra.text = "Created By: " + (string.IsNullOrEmpty(creator.Bio.DisplayName)
                ? creator.Username
                : creator.Bio.DisplayName + " (@" + creator.Username + ")");
            ClickButton.onClick.AddListener(() => AvatarTemplate.Render(avatarMeta, creator, SearchTopBarButton));
        }

        public void Render(WorldMeta worldMeta, User creator)
        {
            ClearLastCard();
            if (!string.IsNullOrEmpty(worldMeta.ThumbnailURL))
                DownloadTools.DownloadBytes(worldMeta.ThumbnailURL, bytes =>
                {
                    if (GifRenderer.IsGif(bytes))
                    {
                        gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                        gifRenderer.LoadGif(bytes);
                    }
                    else
                        Banner.texture = ImageTools.BytesToTexture2D(worldMeta.ThumbnailURL, bytes);
                });
            else
                Banner.texture = DefaultBanner;
            Name.text = worldMeta.Name;
            Extra.text = "Created By: " + (string.IsNullOrEmpty(creator.Bio.DisplayName)
                ? creator.Username
                : creator.Bio.DisplayName + " (@" + creator.Username + ")");
            APIPlayer.APIObject.GetPublicInstancesOfWorld(r => OnInstancesResult(r, worldMeta.Id), worldMeta.Id);
            ClickButton.onClick.AddListener(() =>
                WorldTemplate.Render(worldMeta, creator, lastSafeInstances, SearchTopBarButton));
        }

        private void ClearLastCard()
        {
            ClickButton.onClick.RemoveAllListeners();
            if (gifRenderer != null)
            {
                Destroy(gifRenderer);
                gifRenderer = null;
            }
        }
        
        private void OnInstanceHostResult(SafeInstance safeInstance, CallbackResult<GetUserResult> result)
        {
            if (result.success)
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    lastSafeInstances.Add((safeInstance, result.result.UserData))));
            else
                APIPlayer.APIObject.GetUser(r => OnInstanceHostResult(safeInstance, r),
                    safeInstance.InstanceCreatorId, isUserId: true);
        }
        
        private void OnInstancesResult(CallbackResult<InstancesResult> result, string id)
        {
            if (result.success)
            {
                foreach (SafeInstance safeInstance in result.result.SafeInstances)
                {
                    APIPlayer.APIObject.GetUser(r => OnInstanceHostResult(safeInstance, r),
                        safeInstance.InstanceCreatorId, isUserId: true);
                }
            }
            else
                APIPlayer.APIObject.GetPublicInstancesOfWorld(r => OnInstancesResult(r, id), id);
        }

        private string GetTextFromStatus(Status status)
        {
            switch (status) {
                case Status.Online:
                    return "Online";
                case Status.Absent:
                    return "Absent";
                case Status.Party:
                    return "Party";
                case Status.DoNotDisturb:
                    return "Do Not Disturb";
            }
            return "Offline";
        }
    }
}