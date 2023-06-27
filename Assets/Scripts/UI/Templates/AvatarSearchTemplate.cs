using System;
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
    public class AvatarSearchTemplate : MonoBehaviour
    {
        public LoginPageTopBarButton AvatarsPage;
        public RawImage Banner;
        public TMP_Text AvatarName;
        public TMP_Text AvatarCreator;
        public Button ClickButton;
        public Texture2D DefaultAvatarBanner;

        private AvatarMeta lastAvatarMeta;
        private User lastAvatarCreator;
        private GifRenderer gifRenderer;

        private void OnAvatarCreatorUser(CallbackResult<GetUserResult> result)
        {
            if (result.success)
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    lastAvatarCreator = result.result.UserData;
                    AvatarCreator.text = "Avatar By: " + (string.IsNullOrEmpty(lastAvatarCreator.Bio.DisplayName)
                        ? "@" + lastAvatarCreator.Username
                        : $"{lastAvatarCreator.Bio.DisplayName} (@{lastAvatarCreator.Username})");
                }));
            else
                APIPlayer.APIObject.GetUser(OnAvatarCreatorUser, lastAvatarMeta.OwnerId, isUserId: true);
        }

        public void Render(AvatarTemplate avatarTemplate, AvatarMeta avatarMeta)
        {
            lastAvatarMeta = avatarMeta;
            ClickButton.onClick.RemoveAllListeners();
            APIPlayer.APIObject.GetUser(OnAvatarCreatorUser, avatarMeta.OwnerId, isUserId: true);
            ClickButton.onClick.AddListener(() =>
                avatarTemplate.Render(avatarMeta, lastAvatarCreator, AvatarsPage));
            AvatarName.text = avatarMeta.Name;
            if (!string.IsNullOrEmpty(avatarMeta.ImageURL))
                DownloadTools.DownloadBytes(avatarMeta.ImageURL, bytes =>
                {
                    if (GifRenderer.IsGif(bytes))
                    {
                        gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                        gifRenderer.LoadGif(bytes);
                    }
                    else
                        Banner.texture = ImageTools.BytesToTexture2D(bytes);
                });
            else
                Banner.texture = DefaultAvatarBanner;
        }
    }
}