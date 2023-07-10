using System;
using System.Collections.Generic;
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

        private void PushInvite(GotInvite invite, WorldMeta worldMeta, User from,
            (Texture2D, (string, byte[])?) worldBanner, (Texture2D, (string, byte[])?) userIcon)
        {
            UnreadMessages.Enqueue(new MessageMeta(MessageButtons.OK, _ => SocketManager.JoinInstance(new SafeInstance
            {
                GameServerId = invite.toGameServerId,
                InstanceId = invite.toInstanceId
            }))
            {
                LargeImage = worldBanner,
                SmallImage = userIcon,
                Header = "Got Invite to " + worldMeta.Name,
                SubHeader = string.IsNullOrEmpty(from.Bio.DisplayName)
                    ? from.Username
                    : $"{from.Bio.DisplayName} <size=15>@{from.Username}</size>",
                Description = String.Empty,
                OKText = "Join"
            });
            ShowNotification();
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
            SocketManager.OnInvite += invite =>
            {
                Logger.CurrentLogger.Log("Got Invite from " + invite.fromUserId);
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
        }
    }
}