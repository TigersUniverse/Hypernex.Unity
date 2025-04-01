using System;
using Hypernex.Game;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Abstraction
{
    [RequireComponent(typeof(WorldRender))]
    public class InstanceRender : UIRender, IRender<GameInstance>
    {
        public RectTransform Users;
        public TMP_Text HostLabel;
        public TMP_Text PlayerCount;
        
        private WorldRender worldRender;
        
        public void Render(GameInstance instance)
        {
            WorldMeta worldMeta = instance.worldMeta;
            if(worldRender == null || worldMeta == null) return;
            Users.ClearChildren();
            foreach (User connectedUser in instance.ConnectedUsers)
                CreateCurrentInstanceUserCard(connectedUser);
            if (instance.host != null && HostLabel != null)
                HostLabel.text = "Hosted by " + instance.host.GetUserDisplayName();
            else if(HostLabel != null)
                HostLabel.text = String.Empty;
            if (PlayerCount != null)
                PlayerCount.text = instance.ConnectedUsers.Count.ToString();
            worldRender.Render(worldMeta);
        }
        
        private void CreateCurrentInstanceUserCard(User user)
        {
            IRender<User> instanceCard = Defaults.GetRenderer<User>("CurrentInstanceUserCard");
            instanceCard.Render(user);
            Users.AddChild(instanceCard.transform);
        }

        internal override void Initialize()
        {
            if (worldRender == null) worldRender = GetComponent<WorldRender>();
            base.Initialize();
        }
    }
}