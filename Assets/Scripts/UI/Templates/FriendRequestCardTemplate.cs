using System;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UIActions;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Templates
{
    public class FriendRequestCardTemplate : MonoBehaviour
    {
        public TMP_Text UsernameText;
        public TMP_Text StatusText;
        public RawImage PfpImage;
        public Image Status;
        public RawImage BannerImage;

        public Texture2D DefaultPfp;
        public Texture2D DefaultBanner;

        public Button AcceptButton;
        public Button DeclineButton;
        public Button NavigateButton;

        private LoginPageManager loginPageManager;
        private User lastRenderedUser;
        private bool isCompletingAction;

        public void Render(LoginPageManager instance, User user, Action<bool> removeFromList)
        {
            if (!string.IsNullOrEmpty(user.Bio.DisplayName))
                UsernameText.text = user.Bio.DisplayName + " <size=15>@" + user.Username + "</size>";
            else
                UsernameText.text = "@" + user.Username;
            StatusText.text = !string.IsNullOrEmpty(user.Bio.StatusText) ? user.Bio.StatusText : user.Bio.Status.ToString();
            switch (user.Bio.Status)
            {
                case HypernexSharp.APIObjects.Status.Online:
                    Status.color = ColorTools.RGBtoHSV(44, 224, 44);
                    break;
                case HypernexSharp.APIObjects.Status.Absent:
                    Status.color = ColorTools.RGBtoHSV(255, 187, 15);
                    break;
                case HypernexSharp.APIObjects.Status.Party:
                    Status.color = ColorTools.RGBtoHSV(41, 185, 255);
                    break;
                case HypernexSharp.APIObjects.Status.DoNotDisturb:
                    Status.color = ColorTools.RGBtoHSV(224, 44, 44);
                    break;
                default:
                    Status.color = ColorTools.RGBtoHSV(128, 128, 128);
                    break;
            }
            AcceptButton.onClick.RemoveAllListeners();
            AcceptButton.onClick.AddListener(() =>
            {
                if (!isCompletingAction)
                {
                    isCompletingAction = true;
                    APIPlayer.APIObject.AcceptFriendRequest(result =>
                    {
                        if (result.success)
                            QuickInvoke.InvokeActionOnMainThread(removeFromList, true);
                        else
                            Logger.CurrentLogger.Error("Failed to accept friend request for user " + user.Username);
                        isCompletingAction = false;
                    }, APIPlayer.APIUser, APIPlayer.CurrentToken, user.Id);
                }
            });
            DeclineButton.onClick.RemoveAllListeners();
            DeclineButton.onClick.AddListener(() =>
            {
                if (!isCompletingAction)
                {
                    isCompletingAction = true;
                    APIPlayer.APIObject.DeclineFriendRequest(result =>
                    {
                        if (result.success)
                            QuickInvoke.InvokeActionOnMainThread(removeFromList, false);
                        else
                            Logger.CurrentLogger.Error("Failed to decline friend request for user " + user.Username);
                        isCompletingAction = false;
                    }, APIPlayer.APIUser, APIPlayer.CurrentToken, user.Id);
                }
            });
            if (!string.IsNullOrEmpty(user.Bio.PfpURL))
                DownloadTools.DownloadBytes(user.Bio.PfpURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = PfpImage.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            PfpImage.texture = ImageTools.BytesToTexture2D(user.Bio.PfpURL, bytes);
                    });
            else
                PfpImage.texture = DefaultPfp;
            if (!string.IsNullOrEmpty(user.Bio.BannerURL))
                DownloadTools.DownloadBytes(user.Bio.BannerURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = BannerImage.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            BannerImage.texture = ImageTools.BytesToTexture2D(user.Bio.BannerURL, bytes);
                    });
            else
                BannerImage.texture = DefaultBanner;
            loginPageManager = instance;
            lastRenderedUser = user;
        }
    
        private void Start() => NavigateButton.onClick.AddListener(() =>
        {
            if (lastRenderedUser != null)
                loginPageManager.ProfileTemplate.Render(lastRenderedUser);
        });
    }
}
