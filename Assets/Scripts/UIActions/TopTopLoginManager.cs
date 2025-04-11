using System;
using System.Collections.Generic;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using Hypernex.UIActions.Data;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UIActions
{
    public class TopTopLoginManager : MonoBehaviour
    {
        public static Queue<MessageMeta> UnreadMessages = new();

        public MessagesPageManager MessagesPage;
        public GameObject MailIcon;
        public GameObject NotificationIcon;
        public Texture2D DefaultWorldBanner;
        public Texture2D DefaultUserIcon;

        public void ShowNotification() => NotificationIcon.SetActive(true);
        public void HideNotification() => NotificationIcon.SetActive(false);

        public void ViewMessages()
        {
            MessagesPage.Show(this);
            HideNotification();
        }
        
        private void PushInviteRequest(User from, (Texture2D, (string, byte[])?) userIcon, string assetToken = "")
        {
            MessageMeta messageMeta = new MessageMeta(MessageUrgency.Info, MessageButtons.OK, _ =>
            {
                /*OverlayManager.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
                {
                    Header = "Sent Invite!",
                    Description = "Sent Invite to " + from.Username
                });*/
                SocketManager.InviteUser(GameInstance.FocusedInstance, from);
            }, 5f)
            {
                LargeImage = userIcon,
                Header = "Invite request from " + from.Username,
                OKText = "Send Invite"
            };
            UnreadMessages.Enqueue(messageMeta);
            ShowNotification();
            //OverlayManager.AddMessageToQueue(messageMeta);
        }

        private void PushInvite(GotInvite invite, WorldMeta worldMeta, User from,
            (Texture2D, (string, byte[])?) worldBanner, (Texture2D, (string, byte[])?) userIcon)
        {
            MessageMeta messageMeta = new MessageMeta(MessageUrgency.Info, MessageButtons.OK, _ =>
            {
                /*OverlayManager.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
                {
                    Header = "Joining Instance...",
                    Description = "Joining " + worldMeta.Name + " invited by " + from.Username
                });*/
                SocketManager.JoinInstance(new SafeInstance
                {
                    GameServerId = invite.toGameServerId,
                    InstanceId = invite.toInstanceId,
                    WorldId = invite.worldId
                });
            }, 5f)
            {
                LargeImage = worldBanner,
                SmallImage = userIcon,
                Header = "Got Invite to " + worldMeta.Name,
                SubHeader = string.IsNullOrEmpty(from.Bio.DisplayName)
                    ? from.Username
                    : $"{from.Bio.DisplayName} <size=15>@{from.Username}</size>",
                Description = string.IsNullOrEmpty(from.Bio.DisplayName)
                    ? from.Username
                    : $"{from.Bio.DisplayName} <size=15>@{from.Username}</size>" + " would like you to join " +
                      $"them in {worldMeta.Name}",
                OKText = "Join"
            };
            UnreadMessages.Enqueue(messageMeta);
            ShowNotification();
            //OverlayManager.AddMessageToQueue(messageMeta);
        }

        private void GetUserIcon(GotInvite gotInvite, WorldMeta worldMeta, (Texture2D, (string, byte[])?) worldBanner)
        {
            APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
            {
                if (!result.success)
                    return;
                if (string.IsNullOrEmpty(result.result.UserData.Bio.PfpURL))
                    PushInvite(gotInvite, worldMeta, result.result.UserData, worldBanner, (DefaultUserIcon, null));
                else
                    DownloadTools.DownloadBytes(result.result.UserData.Bio.PfpURL,
                        bytes => PushInvite(gotInvite, worldMeta, result.result.UserData, worldBanner,
                            (null, (result.result.UserData.Bio.PfpURL, bytes))));
            })), gotInvite.fromUserId, isUserId: true);
        }

        private void Start()
        {
            APIPlayer.OnUser += _ => MailIcon.SetActive(true);
            APIPlayer.OnLogout += () => MailIcon.SetActive(false);
            SocketManager.OnInvite += invite =>
            {
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    WorldTemplate.GetWorldMeta(invite.worldId, meta =>
                    {
                        if (string.IsNullOrEmpty(meta.ThumbnailURL))
                            GetUserIcon(invite, meta, (DefaultWorldBanner, null));
                        else
                            DownloadTools.DownloadBytes(meta.ThumbnailURL,
                                bytes => GetUserIcon(invite, meta, (null, (meta.ThumbnailURL, bytes))));
                    });
                }));
            };
            SocketManager.OnInviteRequest += inviteRequest =>
            {
                // Don't handle if we aren't in an instance, or no Player is present (which shouldn't be possible)
                if(GameInstance.FocusedInstance == null || APIPlayer.APIUser == null) return;
                // We can't send an invite if the world is Owner only and we're not the owner
                if (GameInstance.FocusedInstance.worldMeta.Publicity == WorldPublicity.OwnerOnly &&
                    GameInstance.FocusedInstance.worldMeta.OwnerId != APIPlayer.APIUser.Id) return;
                APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    if (!result.success)
                        return;
                    if (string.IsNullOrEmpty(result.result.UserData.Bio.PfpURL))
                        PushInviteRequest(result.result.UserData, (DefaultUserIcon, null));
                    else
                        DownloadTools.DownloadBytes(result.result.UserData.Bio.PfpURL,
                            bytes => PushInviteRequest(result.result.UserData,
                                (null, (result.result.UserData.Bio.PfpURL, bytes))));
                })), inviteRequest.fromUserId, isUserId: true);
            };
        }
    }
}