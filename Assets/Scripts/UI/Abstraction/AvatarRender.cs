using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Components;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Abstraction
{
    public class AvatarRender : UIRender, IRender<AvatarMeta>
    {
        private static List<AvatarMeta> CachedAvatarMeta = new();
        
        public TMP_Text AvatarName;
        public RawImage Banner;
        public TMP_Text AvatarCreator;
        public TMP_Text DescriptionText;
        public Button EquipAvatarButton;
        public bool HideEquipButton = true;
        internal AvatarMeta meta;
        
        public static void GetAvatarMeta(string avatarId, Action<AvatarMeta> callback)
        {
            if (CachedAvatarMeta.Count(x => x.Id == avatarId) > 0)
            {
                callback.Invoke(CachedAvatarMeta.First(x => x.Id == avatarId));
                return;
            }
            APIPlayer.APIObject.GetAvatarMeta(result =>
            {
                if (result.success)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    {
                        CachedAvatarMeta.Add(result.result.Meta);
                        callback.Invoke(result.result.Meta);
                    }));
                else
                    Logger.CurrentLogger.Error("Failed to get AvatarMeta for " + avatarId);
            }, avatarId);
        }
        
        public void Render(AvatarMeta avatarMeta)
        {
            meta = avatarMeta;
            if(AvatarName != null)
                AvatarName.text = avatarMeta.Name;
            if(Banner != null)
                Banner.RenderNetImage(avatarMeta.ImageURL, Defaults.Instance.DefaultAvatarBanner);
            if(AvatarCreator != null)
                APIPlayer.APIObject.GetUser(creatorCallback =>
                {
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    {
                        if (creatorCallback.success)
                            AvatarCreator.text = $"Created by {creatorCallback.result.UserData.GetUserDisplayName()}";
                    }));
                }, avatarMeta.OwnerId, null, true);
            if (DescriptionText != null)
                DescriptionText.text = avatarMeta.Description;
            if (EquipAvatarButton != null)
                EquipAvatarButton.gameObject.SetActive(avatarMeta.Publicity == AvatarPublicity.Anyone ||
                                                       avatarMeta.OwnerId == APIPlayer.APIUser.Id);
        }

        public void Equip()
        {
            if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null &&
                GameInstance.FocusedInstance.World.LockAvatarSwitching)
                return;
            ConfigManager.SelectedConfigUser.CurrentAvatar = meta.Id;
            if(LocalPlayer.Instance != null)
            {
                LocalPlayer.Instance.LoadAvatar();
                OverlayNotification.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
                {
                    Header = "Equipping Avatar",
                    Description = $"Equipping Avatar {meta.Name}, Please Wait."
                });
            }
            ConfigManager.SaveConfigToFile();
        }

        private void LateUpdate()
        {
            if(!HideEquipButton || EquipAvatarButton == null) return;
            if (GameInstance.FocusedInstance == null || GameInstance.FocusedInstance.World == null)
            {
                EquipAvatarButton.gameObject.SetActive(true);
                return;
            }
            EquipAvatarButton.gameObject.SetActive(!GameInstance.FocusedInstance.World.LockAvatarSwitching);
        }
    }
}