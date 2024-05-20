using System;
using System.Collections.Generic;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Player;
using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.UIActions;
using Hypernex.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class ProfileTemplate : MonoBehaviour
    {
        public LoginPageTopBarButton ProfilePage;
        public MessageOverlayTemplate MessageOverlayTemplate;
    
        public RawImage Banner;
        public RawImage Pfp;
        public Image Status;
        public GameObject PronounContainer;
        public TMP_Text Username;
        public TMP_Text StatusText;
        public TMP_Text DescriptionText;
        public Texture2D DefaultPfp;
        public Texture2D DefaultBanner;
        public Button AddFriendButton;
        public Button RemoveFriendButton;
        public Button BlockButton;
        public Button UnblockButton;
        public Button FollowButton;
        public Button UnfollowButton;
        public Button InviteButton;
        public Button RequestInviteButton;
        public Button WarnButton;
        public Button KickButton;
        public Button BanButton;
        public Button UnbanButton;
        public Button AddModeratorButton;
        public Button RemoveModeratorButton;
        public Slider VolumeSlider;
        public List<GameObject> ExtraRemovals = new();

        private TMP_Text pronounText;
        private User lastUser;

        private void SetButtonVisibility(User UserBeingViewed)
        {
            if (UserBeingViewed.Id != APIPlayer.APIUser.Id)
            {
                AddFriendButton.gameObject.SetActive(false);
                RemoveFriendButton.gameObject.SetActive(false);
                BlockButton.gameObject.SetActive(false);
                UnblockButton.gameObject.SetActive(false);
                FollowButton.gameObject.SetActive(false);
                UnfollowButton.gameObject.SetActive(false);
                if(APIPlayer.APIUser.Friends.Contains(UserBeingViewed.Id))
                    RemoveFriendButton.gameObject.SetActive(true);
                else
                    AddFriendButton.gameObject.SetActive(true);
                if(APIPlayer.APIUser.BlockedUsers.Contains(UserBeingViewed.Id))
                    UnblockButton.gameObject.SetActive(true);
                else
                    BlockButton.gameObject.SetActive(true);
                if(APIPlayer.APIUser.Following.Contains(UserBeingViewed.Id))
                    UnfollowButton.gameObject.SetActive(true);
                else
                    FollowButton.gameObject.SetActive(true);
                if (GameInstance.FocusedInstance != null)
                {
                    InviteButton.gameObject.SetActive(GameInstance.FocusedInstance.CanInvite);
                    WarnButton.gameObject.SetActive(GameInstance.FocusedInstance.IsModerator);
                    KickButton.gameObject.SetActive(GameInstance.FocusedInstance.IsModerator);
                    BanButton.gameObject.SetActive(GameInstance.FocusedInstance.IsModerator &&
                                                   !GameInstance.FocusedInstance.BannedUsers.Contains(UserBeingViewed.Id));
                    UnbanButton.gameObject.SetActive(GameInstance.FocusedInstance.IsModerator &&
                                                     GameInstance.FocusedInstance.BannedUsers.Contains(UserBeingViewed.Id));
                    AddModeratorButton.gameObject.SetActive(GameInstance.FocusedInstance.IsModerator &&
                                                            !GameInstance.FocusedInstance.Moderators.Contains(
                                                                UserBeingViewed.Id));
                    RemoveModeratorButton.gameObject.SetActive(GameInstance.FocusedInstance.instanceCreatorId ==
                        APIPlayer.APIUser.Id && GameInstance.FocusedInstance.Moderators.Contains(UserBeingViewed.Id));
                }
                else
                {
                    InviteButton.gameObject.SetActive(false);
                    WarnButton.gameObject.SetActive(false);
                    KickButton.gameObject.SetActive(false);
                    BanButton.gameObject.SetActive(false);
                    UnbanButton.gameObject.SetActive(false);
                    AddModeratorButton.gameObject.SetActive(false);
                    RemoveModeratorButton.gameObject.SetActive(false);
                }
                RequestInviteButton.gameObject.SetActive(UserBeingViewed.isInWorld &&
                                                         APIPlayer.APIUser.Friends.Contains(UserBeingViewed.Id));
                if (ConfigManager.SelectedConfigUser != null &&
                    ConfigManager.SelectedConfigUser.UserVolumes.ContainsKey(UserBeingViewed.Id))
                    VolumeSlider.value = ConfigManager.SelectedConfigUser.UserVolumes[UserBeingViewed.Id];
                else
                    VolumeSlider.value = 1f;
                ExtraRemovals.ForEach(x => x.SetActive(true));
            }
            else
            {
                AddFriendButton.gameObject.SetActive(false);
                RemoveFriendButton.gameObject.SetActive(false);
                BlockButton.gameObject.SetActive(false);
                UnblockButton.gameObject.SetActive(false);
                FollowButton.gameObject.SetActive(false);
                UnfollowButton.gameObject.SetActive(false);
                InviteButton.gameObject.SetActive(false);
                WarnButton.gameObject.SetActive(false);
                KickButton.gameObject.SetActive(false);
                BanButton.gameObject.SetActive(false);
                UnbanButton.gameObject.SetActive(false);
                AddModeratorButton.gameObject.SetActive(false);
                RemoveModeratorButton.gameObject.SetActive(false);
                VolumeSlider.gameObject.SetActive(false);
                ExtraRemovals.ForEach(x => x.SetActive(false));
            }
        }
        
        private void RegisterButtonEvents(User UserBeingViewed)
        {
            AddFriendButton.onClick.AddListener(() =>
            {
                AddFriendButton.gameObject.SetActive(false);
                APIPlayer.APIObject.SendFriendRequest(
                    result =>
                    {
                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                            RemoveFriendButton.gameObject.SetActive(true)));
                        APIPlayer.RefreshUser();
                    }, APIPlayer.APIUser, APIPlayer.CurrentToken, UserBeingViewed.Id);
            });
            RemoveFriendButton.onClick.AddListener(() =>
            {
                RemoveFriendButton.gameObject.SetActive(false);
                APIPlayer.APIObject.RemoveFriend(
                    result =>
                    {
                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                            AddFriendButton.gameObject.SetActive(true)));
                        APIPlayer.RefreshUser();
                    }, APIPlayer.APIUser, APIPlayer.CurrentToken, UserBeingViewed.Id);
            });
            BlockButton.onClick.AddListener(() =>
            {
                BlockButton.gameObject.SetActive(false);
                APIPlayer.APIObject.BlockUser(
                    result =>
                    {
                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                            UnblockButton.gameObject.SetActive(true)));
                        APIPlayer.RefreshUser();
                    }, APIPlayer.APIUser, APIPlayer.CurrentToken, UserBeingViewed.Id);
            });
            UnblockButton.onClick.AddListener(() =>
            {
                UnblockButton.gameObject.SetActive(false);
                APIPlayer.APIObject.UnblockUser(
                    result =>
                    {
                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                            BlockButton.gameObject.SetActive(true)));
                        APIPlayer.RefreshUser();
                    }, APIPlayer.APIUser, APIPlayer.CurrentToken, UserBeingViewed.Id);
            });
            FollowButton.onClick.AddListener(() =>
            {
                FollowButton.gameObject.SetActive(false);
                APIPlayer.APIObject.FollowUser(
                    result =>
                    {
                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                            UnfollowButton.gameObject.SetActive(true)));
                        APIPlayer.RefreshUser();
                    }, APIPlayer.APIUser, APIPlayer.CurrentToken, UserBeingViewed.Id);
            });
            UnfollowButton.onClick.AddListener(() =>
            {
                UnfollowButton.gameObject.SetActive(false);
                APIPlayer.APIObject.FollowUser(
                    result =>
                    {
                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                            FollowButton.gameObject.SetActive(true)));
                        APIPlayer.RefreshUser();
                    }, APIPlayer.APIUser, APIPlayer.CurrentToken, UserBeingViewed.Id);
            });
            InviteButton.onClick.AddListener(() => GameInstance.FocusedInstance.InviteUser(lastUser));
            RequestInviteButton.onClick.AddListener(() => SocketManager.RequestInvite(UserBeingViewed));
            WarnButton.onClick.AddListener(() => MessageOverlayTemplate.Render("Warn", lastUser.Username,
                message => GameInstance.FocusedInstance.WarnUser(lastUser, message)));
            KickButton.onClick.AddListener(() => MessageOverlayTemplate.Render("Kick", lastUser.Username,
                message => GameInstance.FocusedInstance.KickUser(lastUser, message)));
            BanButton.onClick.AddListener(() => MessageOverlayTemplate.Render("Ban", lastUser.Username, message =>
            {
                GameInstance.FocusedInstance.BanUser(lastUser, message);
                UnbanButton.gameObject.SetActive(true);
                BanButton.gameObject.SetActive(false);
            }));
            UnbanButton.onClick.AddListener(() =>
            {
                GameInstance.FocusedInstance.UnbanUser(lastUser);
                BanButton.gameObject.SetActive(true);
                UnbanButton.gameObject.SetActive(false);
            });
            AddModeratorButton.onClick.AddListener(() =>
            {
                GameInstance.FocusedInstance.AddModerator(lastUser);
                RemoveModeratorButton.gameObject.SetActive(true);
                AddModeratorButton.gameObject.SetActive(false);
            });
            RemoveModeratorButton.onClick.AddListener(() =>
            {
                GameInstance.FocusedInstance.RemoveModerator(lastUser);
                AddModeratorButton.gameObject.SetActive(true);
                RemoveModeratorButton.gameObject.SetActive(false);
            });
            VolumeSlider.onValueChanged.AddListener(v =>
            {
                if(ConfigManager.SelectedConfigUser == null)
                    return;
                if (ConfigManager.SelectedConfigUser.UserVolumes.ContainsKey(lastUser.Id))
                {
                    ConfigManager.SelectedConfigUser.UserVolumes[lastUser.Id] = v;
                    return;
                }
                ConfigManager.SelectedConfigUser.UserVolumes.Add(lastUser.Id, v);
            });
        }

        private void DeregisterButtonEvents()
        {
            AddFriendButton.onClick.RemoveAllListeners();
            RemoveFriendButton.onClick.RemoveAllListeners();
            BlockButton.onClick.RemoveAllListeners();
            UnblockButton.onClick.RemoveAllListeners();
            FollowButton.onClick.RemoveAllListeners();
            UnfollowButton.onClick.RemoveAllListeners();
            InviteButton.onClick.RemoveAllListeners();
            WarnButton.onClick.RemoveAllListeners();
            KickButton.onClick.RemoveAllListeners();
            BanButton.onClick.RemoveAllListeners();
            UnbanButton.onClick.RemoveAllListeners();
            AddModeratorButton.onClick.RemoveAllListeners();
            RemoveModeratorButton.onClick.RemoveAllListeners();
            VolumeSlider.onValueChanged.RemoveAllListeners();
        }
    
        public void Render(User user, bool skipShow = false)
        {
            RegisterButtonEvents(user);
            SetButtonVisibility(user);
            if (!string.IsNullOrEmpty(user.Bio.DisplayName))
                Username.text = user.Bio.DisplayName + " <size=15>@" + user.Username + "</size>";
            else
                Username.text = "@" + user.Username;
            StatusText.text = !string.IsNullOrEmpty(user.Bio.StatusText) ? user.Bio.StatusText : user.Bio.Status.ToString();
            DescriptionText.text = user.Bio.Description;
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
            if (user.Bio.Pronouns != null)
            {
                (pronounText == null ? pronounText = PronounContainer.transform.GetChild(0).GetComponent<TMP_Text>() : pronounText)!.text =
                    user.Bio.Pronouns.ToString();
                PronounContainer.SetActive(true);
            }
            else
                PronounContainer.SetActive(false);
            if(ComponentTools.HasComponent<GifRenderer>(Pfp.gameObject))
                Destroy(Pfp.gameObject.GetComponent<GifRenderer>());
            if(ComponentTools.HasComponent<GifRenderer>(Banner.gameObject))
                Destroy(Banner.gameObject.GetComponent<GifRenderer>());
            if (!string.IsNullOrEmpty(user.Bio.PfpURL))
                DownloadTools.DownloadBytes(user.Bio.PfpURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = Pfp.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            Pfp.texture = ImageTools.BytesToTexture2D(user.Bio.PfpURL, bytes);
                    });
            else
                Pfp.texture = DefaultPfp;
            if (!string.IsNullOrEmpty(user.Bio.BannerURL))
                DownloadTools.DownloadBytes(user.Bio.BannerURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            Banner.texture = ImageTools.BytesToTexture2D(user.Bio.BannerURL, bytes);
                    });
            else
                Banner.texture = DefaultBanner;
            if(!skipShow)
                ProfilePage.Show();
            lastUser = user;
        }

        private void Start()
        {
            LoginPageTopBarButton.OnPageChanged += page =>
            {
                if (page.PageName != "Profile")
                {
                    DeregisterButtonEvents();
                }
            };
        }
    }
}