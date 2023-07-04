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
    public class WorldSearchTemplate : MonoBehaviour
    {
        public LoginPageManager LoginPageManager;
        public LoginPageTopBarButton WorldsPage;
        public RawImage Banner;
        public TMP_Text WorldName;
        public TMP_Text WorldCreator;
        public Button ClickButton;
        public Texture2D DefaultWorldBanner;

        private WorldMeta lastWorldMeta;
        private User lastWorldCreator;
        private List<(SafeInstance, User)> lastSafeInstances = new();
        private GifRenderer gifRenderer;

        private void OnWorldCreatorUser(CallbackResult<GetUserResult> result)
        {
            if (result.success)
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    lastWorldCreator = result.result.UserData;
                    WorldCreator.text = "World By: " + (string.IsNullOrEmpty(lastWorldCreator.Bio.DisplayName)
                        ? "@" + lastWorldCreator.Username
                        : $"{lastWorldCreator.Bio.DisplayName} (@{lastWorldCreator.Username})");
                }));
            else
                APIPlayer.APIObject.GetUser(OnWorldCreatorUser, lastWorldMeta.OwnerId, isUserId: true);
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

        private void OnInstancesResult(CallbackResult<InstancesResult> result)
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
                APIPlayer.APIObject.GetPublicInstancesOfWorld(OnInstancesResult, lastWorldMeta.Id);
        }

        public void Render(WorldTemplate worldTemplate, WorldMeta worldMeta)
        {
            lastWorldMeta = worldMeta;
            ClickButton.onClick.RemoveAllListeners();
            APIPlayer.APIObject.GetUser(OnWorldCreatorUser, worldMeta.OwnerId, isUserId: true);
            APIPlayer.APIObject.GetPublicInstancesOfWorld(OnInstancesResult, worldMeta.Id);
            ClickButton.onClick.AddListener(() =>
                worldTemplate.Render(worldMeta, lastWorldCreator, lastSafeInstances, WorldsPage));
            WorldName.text = worldMeta.Name;
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
                Banner.texture = DefaultWorldBanner;
        }
    }
}