using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Configuration;
using Hypernex.Databasing;
using Hypernex.Databasing.Objects;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using Hypernex.UI.Components;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Pages
{
    [RequireComponent(typeof(UserRender))]
    public class ProfilePage : UIPage
    {
        public List<GameObject> HideIfLocal = new();
        public UIThemeObject FriendButton;
        public TMP_Text FriendText;
        public UIThemeObject FollowButton;
        public TMP_Text FollowText;
        public UIThemeObject BlockButton;
        public TMP_Text BlockText;
        public UIThemeObject BanButton;
        public TMP_Text BanText;
        public UIThemeObject KickButton;
        public UIThemeObject InviteButton;
        public UIThemeObject RequestButton;
        public UIThemeObject ModButton;
        public TMP_Text ModText;
        public UIThemeObject WarnButton;
        public RectTransform BadgesContainer;
        public Slider VolumeSlider;
        public TMP_Text VolumeText;
        public ModerateWindow ModerateWindow;
        
        public User UserToRender;

        private UserRender userRender;
        private Dictionary<UIThemeObject, ButtonType> cachedButtonTypes = new();
        private bool isInit;

        private string GetTextFromButtonState(UIThemeObject button, bool v, string t, string f)
        {
            if(!cachedButtonTypes.ContainsKey(button)) cachedButtonTypes.Add(button, button.ButtonType);
            button.ButtonType = v ? cachedButtonTypes[button] : ButtonType.Grey;
            button.ApplyTheme(UITheme.SelectedTheme);
            return v ? t : f;
        }

        private void RenderPage()
        {
            isInit = true;
            User localUser = APIPlayer.APIUser;
            if (UserToRender != localUser)
            {
                FriendButton.gameObject.SetActive(true);
                FriendText.text = GetTextFromButtonState(FriendButton, !localUser.Friends.Contains(UserToRender.Id),
                    "Friend", "Unfriend");
                FollowButton.gameObject.SetActive(true);
                FollowText.text = GetTextFromButtonState(FollowButton, !localUser.Following.Contains(UserToRender.Id),
                    "Follow", "Unfollow");
                BlockButton.gameObject.SetActive(true);
                BlockText.text = GetTextFromButtonState(BlockButton, !localUser.BlockedUsers.Contains(UserToRender.Id),
                    "Block", "Unblock");
                GameInstance gameInstance = GameInstance.FocusedInstance;
                if (gameInstance != null && (gameInstance.IsModerator || gameInstance.isHost))
                {
                    BanButton.gameObject.SetActive(true);
                    BanText.text = GetTextFromButtonState(BanButton,
                        !gameInstance.BannedUsers.Contains(UserToRender.Id), "Ban", "Unban");
                    KickButton.gameObject.SetActive(gameInstance.SocketConnectedUsers.Contains(UserToRender.Id));
                    if (gameInstance.isHost)
                    {
                        ModButton.gameObject.SetActive(true);
                        ModText.text = GetTextFromButtonState(ModButton,
                            !gameInstance.Moderators.Contains(UserToRender.Id), "Mod", "Unmod");
                    }
                    else
                        ModButton.gameObject.SetActive(false);
                    WarnButton.gameObject.SetActive(true);
                }
                else
                {
                    BanButton.gameObject.SetActive(false);
                    KickButton.gameObject.SetActive(false);
                    ModButton.gameObject.SetActive(false);
                    WarnButton.gameObject.SetActive(false);
                }
                InviteButton.gameObject.SetActive(gameInstance != null && gameInstance.CanInvite);
                RequestButton.gameObject.SetActive(true);
                try
                {
                    Database database = ConfigManager.GetDatabase();
                    if (database != null)
                    {
                        PlayerOverrides playerOverrides =
                            database.Get<PlayerOverrides>(PlayerOverrides.TABLE, UserToRender.Id);
                        VolumeSlider.value = playerOverrides?.Volume ?? 1;
                        VolumeText.text = playerOverrides != null
                            ? $"{(int) Mathf.Round(playerOverrides.Volume * 100f)}%"
                            : "100%";
                    }
                } catch(Exception){}
                HideIfLocal.ForEach(x => x.SetActive(true));
            }
            else
            {
                FriendButton.gameObject.SetActive(false);
                FollowButton.gameObject.SetActive(false);
                BlockButton.gameObject.SetActive(false);
                BanButton.gameObject.SetActive(false);
                KickButton.gameObject.SetActive(false);
                ModButton.gameObject.SetActive(false);
                WarnButton.gameObject.SetActive(false);
                InviteButton.gameObject.SetActive(false);
                RequestButton.gameObject.SetActive(false);
                HideIfLocal.ForEach(x => x.SetActive(false));
            }
            BadgesContainer.ClearChildren();
            if (UserToRender.Badges == null)
                Logger.CurrentLogger.Warn("[API] User " + UserToRender.Id + " has null Badges! This is usually an API fault. Please report this to system administrators!");
            else
            {
                UserToRender.Badges.ForEach(
                    x => BadgeRankHandler.GetBadgeHandlerByName(x)?.ApplyToProfile(this, UserToRender));
                BadgeRankHandler.GetRankHandlersByRank(UserToRender.Rank).ForEach(x => x.ApplyToProfile(this, UserToRender));
            }
            isInit = false;
        }

        private void AfterAction() => APIPlayer.RefreshUser(_ => RenderPage());

        public void Default() => UserToRender = APIPlayer.APIUser;

        public void Friend()
        {
            User localUser = APIPlayer.APIUser;
            if(UserToRender == localUser) return;
            FriendButton.gameObject.SetActive(false);
            if (!localUser.Friends.Contains(UserToRender.Id))
            {
                APIPlayer.APIObject.SendFriendRequest(_ => AfterAction(), localUser, APIPlayer.CurrentToken,
                    UserToRender.Id);
            }
            else
            {
                APIPlayer.APIObject.RemoveFriend(_ => AfterAction(), localUser, APIPlayer.CurrentToken,
                    UserToRender.Id);
            }
        }

        public void Follow()
        {
            User localUser = APIPlayer.APIUser;
            if(UserToRender == localUser) return;
            FollowButton.gameObject.SetActive(false);
            if (!localUser.Following.Contains(UserToRender.Id))
            {
                APIPlayer.APIObject.FollowUser(_ => AfterAction(), localUser, APIPlayer.CurrentToken, UserToRender.Id);
            }
            else
            {
                APIPlayer.APIObject.UnfollowUser(_ => AfterAction(), localUser, APIPlayer.CurrentToken, UserToRender.Id);
            }
        }

        public void Block()
        {
            User localUser = APIPlayer.APIUser;
            if(UserToRender == localUser) return;
            BlockButton.gameObject.SetActive(false);
            if (!localUser.BlockedUsers.Contains(UserToRender.Id))
            {
                APIPlayer.APIObject.BlockUser(_ => AfterAction(), localUser, APIPlayer.CurrentToken, UserToRender.Id);
            }
            else
            {
                APIPlayer.APIObject.UnblockUser(_ => AfterAction(), localUser, APIPlayer.CurrentToken, UserToRender.Id);
            }
        }
        
        public void Ban()
        {
            if(UserToRender == APIPlayer.APIUser) return;
            GameInstance gameInstance = GameInstance.FocusedInstance;
            if(gameInstance == null) return;
            if (gameInstance.BannedUsers.Contains(UserToRender.Id))
            {
                gameInstance.UnbanUser(UserToRender);
                AfterAction();
            }
            else
                ModerateWindow.Apply("Ban", UserToRender.Username, s =>
                {
                    BanButton.gameObject.SetActive(false);
                    if(!gameInstance.BannedUsers.Contains(UserToRender.Id))
                    {
                        gameInstance.BanUser(UserToRender, s);
                    }
                    else
                    {
                        gameInstance.UnbanUser(UserToRender);
                    }
                    AfterAction();
                });
        }

        public void Kick()
        {
            if(UserToRender == APIPlayer.APIUser) return;
            GameInstance gameInstance = GameInstance.FocusedInstance;
            if(gameInstance == null) return;
            ModerateWindow.Apply("Kick", UserToRender.Username, s =>
            {
                KickButton.gameObject.SetActive(false);
                gameInstance.KickUser(UserToRender, s);
                AfterAction();
            });
        }

        public void Warn()
        {
            if(UserToRender == APIPlayer.APIUser) return;
            GameInstance gameInstance = GameInstance.FocusedInstance;
            if(gameInstance == null) return;
            ModerateWindow.Apply("Warn", UserToRender.Username, s =>
            {
                WarnButton.gameObject.SetActive(false);
                gameInstance.WarnUser(UserToRender, s);
                AfterAction();
            });
        }

        public void Invite()
        {
            if(UserToRender == APIPlayer.APIUser) return;
            GameInstance gameInstance = GameInstance.FocusedInstance;
            if(gameInstance == null) return;
            InviteButton.gameObject.SetActive(false);
            gameInstance.InviteUser(UserToRender);
        }
        
        public void Mod()
        {
            if(UserToRender == APIPlayer.APIUser) return;
            GameInstance gameInstance = GameInstance.FocusedInstance;
            if(gameInstance == null) return;
            ModButton.gameObject.SetActive(false);
            if(!gameInstance.Moderators.Contains(UserToRender.Id))
            {
                gameInstance.AddModerator(UserToRender);
            }
            else
            {
                gameInstance.RemoveModerator(UserToRender);
            }
            AfterAction();
        }

        public void RequestInvite()
        {
            if(UserToRender == APIPlayer.APIUser) return;
            RequestButton.gameObject.SetActive(false);
            SocketManager.RequestInvite(UserToRender);
        }
        
        public void OnSelectUser(UserRender u)
        {
            UserToRender = u.u;
            Show();
        }

        public void OnUserVolumeChanged(float v)
        {
            if(isInit || UserToRender == APIPlayer.APIUser) return;
            Database database = ConfigManager.GetDatabase();
            if(database == null) return;
            if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.ConnectedUsers.Count(x => x.Id == UserToRender.Id) > 0)
            {
                NetPlayer netPlayer = PlayerManagement.GetNetPlayer(GameInstance.FocusedInstance, UserToRender.Id);
                if (netPlayer != null)
                {
                    netPlayer.PlayerOverrides.Volume = v;
                    database.Insert(PlayerOverrides.TABLE, netPlayer.PlayerOverrides);
                    return;
                }
            }
            PlayerOverrides playerOverrides =
                database.Get<PlayerOverrides>(PlayerOverrides.TABLE, UserToRender.Id);
            if (playerOverrides != null)
            {
                playerOverrides.Volume = v;
                database.Insert(PlayerOverrides.TABLE, playerOverrides);
            }
            else
            {
                playerOverrides = new PlayerOverrides(UserToRender.Id);
                playerOverrides.Volume = v;
                database.Insert(PlayerOverrides.TABLE, playerOverrides);
            }
            VolumeText.text = $"{(int) Mathf.Round(v * 100f)}%";
        }

        public override void Show(bool hideAll = true)
        {
            base.Show(hideAll);
            if(UserToRender == null) return;
            userRender.Render(UserToRender);
            RenderPage();
        }

        internal override void Initialize()
        {
            if(userRender == null) userRender = GetComponent<UserRender>();
            base.Initialize();
        }
    }
}