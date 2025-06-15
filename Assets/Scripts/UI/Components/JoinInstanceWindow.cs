using System;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Components
{
    public class JoinInstanceWindow : MonoBehaviour
    {
        public TMP_Text InstanceInfoText;
        public RectTransform UserList;
        
        private SafeInstance safeInstance;
        private User creator;
        private WorldMeta worldMeta;
        
        private void CreateUserInstanceCard(User user)
        {
            IRender<User> instanceRender = Defaults.GetRenderer<User>("UserInstanceTemplate");
            instanceRender.Render(user);
            UserList.AddChild(instanceRender.transform);
        }
        
        public void Apply(SafeInstance s, User c, WorldMeta w)
        {
            safeInstance = s;
            creator = c;
            worldMeta = w;
            if (InstanceInfoText != null)
                InstanceInfoText.text = $"{w.Name}\n@{c.Username}";
            if(UserList == null) return;
            UserList.ClearChildren();
            foreach (string connectedUser in s.ConnectedUsers)
            {
                APIPlayer.APIObject.GetUser(r => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    if (!r.success || r.result == null) return;
                    CreateUserInstanceCard(r.result.UserData);
                })), connectedUser, null, true);
            }
        }

        public void Join()
        {
            if (GameInstance.FocusedInstance != null &&
                GameInstance.FocusedInstance.gameServerId == safeInstance.GameServerId &&
                GameInstance.FocusedInstance.instanceId == safeInstance.InstanceId)
                return;
            OverlayNotification.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
            {
                Header = "Joining Instance",
                Description = $"Joining instance for World {worldMeta.Name}, hosted by {creator.Username}, Please Wait"
            });
            Return();
            SocketManager.JoinInstance(safeInstance);
        }

        public void Return() => gameObject.SetActive(false);
    }
}