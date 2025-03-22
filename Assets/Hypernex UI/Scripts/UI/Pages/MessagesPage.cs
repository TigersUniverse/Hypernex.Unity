using System;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using UnityEngine;

namespace Hypernex.UI.Pages
{
    public class MessagesPage : UIPage
    {
        public RectTransform MessagesScroll;
        
        public void PushInviteRequest(User from)
        {
            GetResourceFromUrl(from.Bio.BannerURL, userIcon =>
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
                PushMessage(messageMeta);
            }, Defaults.Instance.DefaultProfileBanner);
        }

        public void PushInvite(GotInvite invite, WorldMeta worldMeta, User from)
        {
            GetResourceFromUrl(worldMeta.ThumbnailURL, worldBanner =>
            {
                GetResourceFromUrl(from.Bio.PfpURL, userIcon =>
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
                    PushMessage(messageMeta);
                }, Defaults.Instance.DefaultProfilePicture);
            }, Defaults.Instance.DefaultWorldBanner);
        }

        public void PushMessage(MessageMeta messageMeta)
        {
            IRender<MessageMeta> message = Defaults.GetRenderer<MessageMeta>("MessageTemplate");
            message.Render(messageMeta);
            MessagesScroll.AddChild(message.transform);
            message.transform.SetAsFirstSibling();
            // TODO: Move to new OverlayManager for top notifications bar
            /*UnreadMessages.Enqueue(messageMeta);
            ShowNotification();
            OverlayManager.AddMessageToQueue(messageMeta);*/
        }

        public void ClearMessages() => MessagesScroll.ClearChildren();
        
        private static void GetResourceFromUrl(string url, Action<(Texture2D, (string, byte[])?)> callback, Texture2D d)
        {
            if (string.IsNullOrEmpty(url))
            {
                (Texture2D, (string, byte[])?) result = (d, null);
                callback.Invoke(result);
                return;
            }
            DownloadTools.DownloadBytes(url, bytes =>
            {
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    (Texture2D, (string, byte[])?) result = (null, (url, bytes));
                    callback.Invoke(result);
                }));
            });
        }
    }
}