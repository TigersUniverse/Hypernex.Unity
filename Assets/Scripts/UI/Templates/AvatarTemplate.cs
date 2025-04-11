using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UIActions;
using Hypernex.UIActions.Data;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Templates
{
    public class AvatarTemplate : MonoBehaviour
    {
        private static List<AvatarMeta> CachedAvatarMeta = new();

        public LoginPageTopBarButton AvatarPage;
        public TMP_Text AvatarName;
        public RawImage Banner;
        public TMP_Text AvatarCreator;
        public TMP_Text DescriptionText;
        public Button EquipAvatarButton;
        public Button ReturnButton;
        public Button FavoriteButton;
        public TMP_Text FavoriteButtonText;
        public Texture2D DefaultIcon;

        private AvatarMeta lastAvatarMeta;
        private User lastCreator;
        private LoginPageTopBarButton previousPage;

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

        public void Render(AvatarMeta avatarMeta, User creator, LoginPageTopBarButton pp)
        {
            AvatarName.text = avatarMeta.Name;
            if (!string.IsNullOrEmpty(avatarMeta.ImageURL))
            {
                DownloadTools.DownloadBytes(avatarMeta.ImageURL, bytes =>
                {
                    if (GifRenderer.IsGif(bytes))
                    {
                        GifRenderer gifRenderer = Banner.gameObject.GetComponent<GifRenderer>();
                        if (gifRenderer != null)
                        {
                            Destroy(gifRenderer);
                            gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                        }
                        gifRenderer!.LoadGif(bytes);
                    }
                    else
                        Banner.texture = ImageTools.BytesToTexture2D(avatarMeta.ImageURL, bytes);
                });
            }
            else
                Banner.texture = DefaultIcon;
            AvatarCreator.text = $"Created By {creator.Username}";
            DescriptionText.text = avatarMeta.Description;
            EquipAvatarButton.gameObject.SetActive(avatarMeta.Publicity == AvatarPublicity.Anyone ||
                                                   avatarMeta.OwnerId == APIPlayer.APIUser.Id);
            lastAvatarMeta = avatarMeta;
            lastCreator = creator;
            previousPage = pp;
            FavoriteButtonText.text = ConfigManager.SelectedConfigUser.SavedAvatars.Contains(avatarMeta.Id)
                ? "Unfavorite"
                : "Favorite";
            FavoriteButton.onClick.RemoveAllListeners();
            FavoriteButton.onClick.AddListener(() =>
            {
                if (!ConfigManager.SelectedConfigUser.SavedAvatars.Contains(avatarMeta.Id))
                {
                    ConfigManager.SelectedConfigUser.SavedAvatars.Add(avatarMeta.Id);
                    ConfigManager.SaveConfigToFile();
                    FavoriteButtonText.text = "Unfavorite";
                }
                else
                {
                    ConfigManager.SelectedConfigUser.SavedAvatars.Remove(avatarMeta.Id);
                    ConfigManager.SaveConfigToFile();
                    FavoriteButtonText.text = "Favorite";
                }
            });
            ReturnButton.onClick.RemoveAllListeners();
            ReturnButton.onClick.AddListener(pp.Show);
            AvatarPage.Show();
        }

        private void Start()
        {
            APIPlayer.OnUserRefresh += u => CachedAvatarMeta.Clear();
            EquipAvatarButton.onClick.AddListener(() =>
            {
                if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null &&
                    GameInstance.FocusedInstance.World.LockAvatarSwitching)
                    return;
                //CreateInstanceTemplate.Render(lastAvatarMeta, lastCreator);
                ConfigManager.SelectedConfigUser.CurrentAvatar = lastAvatarMeta.Id;
                if(LocalPlayer.Instance != null)
                {
                    LocalPlayer.Instance.LoadAvatar();
                    OverlayManager.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
                    {
                        Header = "Equipping Avatar",
                        Description = $"Equipping Avatar {lastAvatarMeta.Name}, Please Wait."
                    });
                }
                ConfigManager.SaveConfigToFile();
                previousPage.Show();
            });
        }

        private void Update()
        {
            if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null &&
                GameInstance.FocusedInstance.World.LockAvatarSwitching)
            {
                EquipAvatarButton.gameObject.SetActive(false);
                return;
            }
            EquipAvatarButton.gameObject.SetActive(true);
        }
    }
}