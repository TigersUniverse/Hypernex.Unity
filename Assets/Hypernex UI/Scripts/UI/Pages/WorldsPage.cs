using System;
using System.Collections.Generic;
using Hypernex.Configuration;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using Hypernex.UI.Components;
using HypernexSharp.API;
using HypernexSharp.API.APIMessages;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Pages
{
    public class WorldsPage : UIPage
    {
        public TMP_Dropdown PopularityDropdown;
        public TMP_Text HeaderText;
        public WorldRender SelectedWorld;
        public ToggleButton[] CategoryToggles;
        public RectTransform List;
        public TMP_Text FavoriteButtonText;
        
        public void RefreshWorlds()
        {
            ShowSubPage(0);
            List.ClearChildren();
            int i = CategoryToggles.GetSelectedIndex();
            PopularityDropdown.gameObject.SetActive(i == 0);
            switch (i)
            {
                case 0:
                    HeaderText.text = "Popular Worlds";
                    PopularityType popularityType = (PopularityType) PopularityDropdown.value;
                    APIPlayer.APIObject.GetWorldPopularity(OnPopularityResult, popularityType);
                    break;
                case 1:
                    HeaderText.text = "Game Worlds";
                    APIPlayer.APIObject.SearchByTag(OnSearchResult, SearchType.World, "game");
                    break;
                case 2:
                    HeaderText.text = "Favorite Worlds";
                    RenderListedIds(ConfigManager.SelectedConfigUser.SavedWorlds);
                    break;
                case 3:
                    HeaderText.text = "My Worlds";
                    APIPlayer.RefreshUser(u => RenderListedIds(u.Worlds));
                    break;
            }
        }

        public void OnSelectWorld(WorldRender worldRender)
        {
            SelectedWorld.Render(worldRender.meta);
            FavoriteButtonText.text = ConfigManager.SelectedConfigUser.SavedWorlds.Contains(worldRender.meta.Id)
                ? "Unfavorite"
                : "Favorite";
            ShowSubPage(1);
        }

        public void FavoriteWorld()
        {
            WorldMeta worldMeta = SelectedWorld.meta;
            if (!ConfigManager.SelectedConfigUser.SavedWorlds.Contains(worldMeta.Id))
            {
                ConfigManager.SelectedConfigUser.SavedWorlds.Add(worldMeta.Id);
                ConfigManager.SaveConfigToFile();
                FavoriteButtonText.text = "Unfavorite";
            }
            else
            {
                ConfigManager.SelectedConfigUser.SavedWorlds.Remove(worldMeta.Id);
                ConfigManager.SaveConfigToFile();
                FavoriteButtonText.text = "Favorite";
            }
        }
        
        private void OnSearchResult(CallbackResult<SearchResult> callbackResult) =>
            QuickInvoke.InvokeActionOnMainThread(new Action(
                () =>
                {
                    if (!callbackResult.success) return;
                    foreach (string id in callbackResult.result.Candidates)
                    {
                        WorldRender.GetWorldMeta(id, worldMeta =>
                        {
                            if(worldMeta == null)
                                return;
                            CreateWorldCardTemplate(worldMeta);
                        });
                    }
                }));

        private void OnPopularityResult(CallbackResult<PopularityResult> popularityResult) =>
            QuickInvoke.InvokeActionOnMainThread(new Action(
                () =>
                {
                    if(!popularityResult.success) return;
                    GetWorldsInOrder(popularityResult.result.Popularity, new List<WorldMeta>(), result =>
                    {
                        foreach (WorldMeta m in result)
                        {
                            if (m == null) continue;
                            CreateWorldCardTemplate(m);
                        }
                    });
                }));

        private void GetWorldsInOrder(Popularity[] popularities, List<WorldMeta> current,
            Action<List<WorldMeta>> onDone)
        {
            if(popularities.Length == current.Count)
                QuickInvoke.InvokeActionOnMainThread(onDone, current);
            else
            {
                Popularity popularity = popularities[current.Count];
                WorldRender.GetWorldMeta(popularity.Id, world =>
                {
                    if (world == null)
                    {
                        current.Add(null);
                        GetWorldsInOrder(popularities, current, onDone);
                        return;
                    }
                    current.Add(world);
                    GetWorldsInOrder(popularities, current, onDone);
                });
            }
        }

        private void RenderListedIds(string[] ids)
        {
            foreach (string id in ids)
            {
                WorldRender.GetWorldMeta(id, worldMeta =>
                {
                    if(worldMeta == null)
                        return;
                    CreateWorldCardTemplate(worldMeta);
                });
            }
        }
        
        private void RenderListedIds(List<string> ids)
        {
            foreach (string id in ids)
            {
                WorldRender.GetWorldMeta(id, worldMeta =>
                {
                    if(worldMeta == null)
                        return;
                    CreateWorldCardTemplate(worldMeta);
                });
            }
        }
        
        private void CreateWorldCardTemplate(WorldMeta worldMeta)
        {
            if(worldMeta == null)
                return;
            IRender<WorldMeta> newWorldCard = Defaults.GetRenderer<WorldMeta>("WorldCardTemplate");
            RectTransform c = newWorldCard.GetComponent<RectTransform>();
            newWorldCard.Render(worldMeta);
            List.AddChild(c);
            c.anchoredPosition = new Vector2(c.anchoredPosition.x, 0);
        }

        private void OnEnable()
        {
            RefreshWorlds();
        }
    }
}