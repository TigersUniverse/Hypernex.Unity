using System;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Components;
using HypernexSharp.APIObjects;
using HypernexSharp.SocketObjects;
using TMPro;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction
{
    public class SafeInstanceRender : UIRender, IRender<SafeInstance>
    {
        public TMP_Text PublicityText;
        public TMP_Text PlayerCountText;
        public TMP_Text InstanceCreatorText;
        public Image PublicityIcon;
        public JoinInstanceWindow JoinWindow;

        private SafeInstance s;
        private User u;
        private WorldMeta w;
        
        public void Render(SafeInstance t)
        {
            s = t;
            if (PublicityText != null)
                PublicityText.text = t.InstancePublicity.ToString();
            if (PublicityIcon != null)
            {
                switch (t.InstancePublicity)
                {
                    case InstancePublicity.Anyone:
                        PublicityIcon.sprite = Defaults.Instance.PublicIcon;
                        break;
                    case InstancePublicity.Acquaintances:
                    case InstancePublicity.Friends:
                        PublicityIcon.sprite = Defaults.Instance.FriendsIcon;
                        break;
                    default:
                        PublicityIcon.sprite = Defaults.Instance.LockedIcon;
                        break;
                }
            }
            if (PlayerCountText != null)
                PlayerCountText.text = t.ConnectedUsers.Count.ToString();
            if(InstanceCreatorText != null)
                APIPlayer.APIObject.GetUser(r => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    if (!r.success || r.result == null)
                    {
                        InstanceCreatorText.text = String.Empty;
                        return;
                    }
                    u = r.result.UserData;
                    InstanceCreatorText.text = "@" + u.Username;
                })), t.InstanceCreatorId, null, true);
            WorldRender.GetWorldMeta(t.WorldId, meta => w = meta);
        }

        public void OnSelect()
        {
            JoinWindow.Apply(s, u, w);
            JoinWindow.gameObject.SetActive(true);
        }
    }
}