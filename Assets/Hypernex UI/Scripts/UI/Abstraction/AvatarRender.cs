using System;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction
{
    public class AvatarRender : MonoBehaviour, IUIRenderer<AvatarMeta>
    {
        public TMP_Text AvatarName;
        public RawImage Banner;
        public TMP_Text AvatarCreator;
        public TMP_Text DescriptionText;
        
        public void Render(AvatarMeta meta)
        {
            if(AvatarName != null)
                AvatarName.text = meta.Name;
            if(Banner != null)
                Banner.RenderNetImage(meta.ImageURL, Defaults.Instance.DefaultAvatarBanner);
            if(AvatarCreator != null)
                APIPlayer.APIObject.GetUser(creatorCallback =>
                {
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    {
                        if (creatorCallback.success)
                            AvatarCreator.text = $"Created by {creatorCallback.result.UserData.GetUserDisplayName()}";
                    }));
                }, meta.OwnerId);
            if (DescriptionText != null)
                DescriptionText.text = meta.Description;
        }
    }
}