using System;
using System.Collections.Generic;
using Hypernex.Configuration;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using Hypernex.UI.Components;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Pages
{
    public class AvatarsPage : UIPage
    {
        public TMP_Dropdown PopularityDropdown;
        public TMP_Text HeaderText;
        public AvatarRender SelectedAvatar;
        public ToggleButton[] CategoryToggles;
        public RectTransform List;
        public TMP_Text FavoriteButtonText;
        public ScrollRect ScrollList;

        private bool isRendering = true;
        private bool local;
        private int page;
        private int lastResultsLength;
        
        public void RefreshAvatars()
        {
            ShowSubPage(0);
            List.ClearChildren();
            page = 0;
            SendRefresh();
        }

        private void SendRefresh()
        {
            int i = CategoryToggles.GetSelectedIndex();
            PopularityDropdown.gameObject.SetActive(i == 0);
            isRendering = true;
            switch (i)
            {
                case 0:
                    HeaderText.text = "Popular Avatars";
                    PopularityType popularityType = (PopularityType) PopularityDropdown.value;
                    APIPlayer.APIObject.GetAvatarPopularity(OnPopularityResult, popularityType, null, Defaults.MAX_RESULTS,
                        page);
                    local = false;
                    break;
                /*case 1:
                    HeaderText.text = ". Avatar";
                    APIPlayer.APIObject.SearchByTag(OnSearchResult, SearchType.Avatar, ".");
                    local = true;
                    break;*/
                case 1:
                    HeaderText.text = "Favorite Avatars";
                    RenderListedIds(ConfigManager.SelectedConfigUser.SavedAvatars);
                    local = true;
                    break;
                case 2:
                    HeaderText.text = "My Avatars";
                    APIPlayer.RefreshUser(u => RenderListedIds(u.Avatars));
                    local = true;
                    break;
            }
        }

        public void OnSelectAvatar(AvatarRender avatarRender)
        {
            SelectedAvatar.Render(avatarRender.meta);
            FavoriteButtonText.text = ConfigManager.SelectedConfigUser.SavedAvatars.Contains(avatarRender.meta.Id)
                ? "Unfavorite"
                : "Favorite";
            ShowSubPage(1);
        }

        public void FavoriteAvatar()
        {
            AvatarMeta avatarMeta = SelectedAvatar.meta;
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
        }
        
        private void OnSearchResult(CallbackResult<SearchResult> callbackResult) =>
            QuickInvoke.InvokeActionOnMainThread(new Action(
                () =>
                {
                    lastResultsLength = callbackResult.result.Candidates.Count;
                    isRendering = false;
                    if (!callbackResult.success) return;
                    foreach (string id in callbackResult.result.Candidates)
                    {
                        AvatarRender.GetAvatarMeta(id, avatarMeta =>
                        {
                            if(avatarMeta == null)
                                return;
                            CreateAvatarCardTemplate(avatarMeta);
                        });
                    }
                }));

        private void OnPopularityResult(CallbackResult<PopularityResult> popularityResult) =>
            QuickInvoke.InvokeActionOnMainThread(new Action(
                () =>
                {
                    if(!popularityResult.success) return;
                    GetAvatarsInOrder(popularityResult.result.Popularity, new List<AvatarMeta>(), result =>
                    {
                        lastResultsLength = result.Count;
                        isRendering = false;
                        foreach (AvatarMeta m in result)
                        {
                            if (m == null) continue;
                            CreateAvatarCardTemplate(m);
                        }
                    });
                }));

        private void GetAvatarsInOrder(Popularity[] popularities, List<AvatarMeta> current,
            Action<List<AvatarMeta>> onDone)
        {
            if(popularities.Length == current.Count)
                QuickInvoke.InvokeActionOnMainThread(onDone, current);
            else
            {
                Popularity popularity = popularities[current.Count];
                AvatarRender.GetAvatarMeta(popularity.Id, avatar =>
                {
                    if (avatar == null)
                    {
                        current.Add(null);
                        GetAvatarsInOrder(popularities, current, onDone);
                        return;
                    }
                    current.Add(avatar);
                    GetAvatarsInOrder(popularities, current, onDone);
                });
            }
        }

        private void RenderListedIds(string[] ids)
        {
            foreach (string id in ids)
            {
                AvatarRender.GetAvatarMeta(id, avatarMeta =>
                {
                    if(avatarMeta == null)
                        return;
                    CreateAvatarCardTemplate(avatarMeta);
                });
            }
            isRendering = false;
            page = 0;
            lastResultsLength = 0;
        }
        
        private void RenderListedIds(List<string> ids)
        {
            foreach (string id in ids)
            {
                AvatarRender.GetAvatarMeta(id, avatarMeta =>
                {
                    if(avatarMeta == null)
                        return;
                    CreateAvatarCardTemplate(avatarMeta);
                });
            }
            isRendering = false;
            page = 0;
            lastResultsLength = 0;
        }
        
        private void CreateAvatarCardTemplate(AvatarMeta avatarMeta)
        {
            if(avatarMeta == null)
                return;
            IRender<AvatarMeta> newAvatarCard = Defaults.GetRenderer<AvatarMeta>("AvatarCardTemplate");
            RectTransform c = newAvatarCard.GetComponent<RectTransform>();
            newAvatarCard.Render(avatarMeta);
            List.AddChild(c);
            c.anchoredPosition = new Vector2(c.anchoredPosition.x, 0);
        }

        private void OnEnable()
        {
            RefreshAvatars();
        }
        
        private void Update()
        {
            if (isRendering || lastResultsLength < Defaults.MAX_RESULTS || local) return;
            Vector2 pos = ScrollList.normalizedPosition;
            if(pos.y >= 0) return;
            page++;
            SendRefresh();
            Debug.Log("Requesting avatar refresh at page " + page);
        }
    }
}