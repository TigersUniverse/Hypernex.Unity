using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Configuration;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI;
using Hypernex.UI.Templates;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UIActions
{
    public class WorldsPageManager : MonoBehaviour
    {
        public LoginPageTopBarButton WorldsPage;
        public WorldTemplate WorldTemplate;
        public TMP_Dropdown PopularityTypeDropdown;
        public DynamicScroll PopularWorlds;
        public DynamicScroll MyWorlds;
        public DynamicScroll FavoritedWorlds;

        private bool isGettingPopular;

        public void Refresh()
        {
            if(!isGettingPopular)
                RefreshPopularWorlds();
            MyWorlds.Clear();
            FavoritedWorlds.Clear();
            foreach (string worldId in APIPlayer.APIUser.Worlds)
                WorldTemplate.GetWorldMeta(worldId, meta =>
                {
                    if (meta.Builds.Count(x => x.BuildPlatform == AssetBundleTools.Platform) > 0)
                        CreateWorldCardTemplate(meta, APIPlayer.APIUser, MyWorlds);
                });
            foreach (string worldId in ConfigManager.SelectedConfigUser.SavedWorlds)
                WorldTemplate.GetWorldMeta(worldId,
                    meta =>
                    {
                        if (meta.Builds.Count(x => x.BuildPlatform == AssetBundleTools.Platform) > 0)
                            APIPlayer.APIObject.GetUser(
                                userResult => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                    CreateWorldCardTemplate(meta, userResult.result.UserData, FavoritedWorlds))),
                                meta.OwnerId,
                                isUserId: true);
                    });
        }

        public void RefreshPopularWorlds()
        {
            if(isGettingPopular)
                return;
            isGettingPopular = true;
            PopularWorlds.Clear();
            PopularityType popularityType = (PopularityType) PopularityTypeDropdown.value;
            APIPlayer.APIObject.GetWorldPopularity(OnPopularityResult, popularityType);
        }

        private void OnPopularityResult(CallbackResult<PopularityResult> popularityResult) =>
            QuickInvoke.InvokeActionOnMainThread(new Action(
                () =>
                {
                    if(!popularityResult.success)
                    {
                        isGettingPopular = false;
                        return;
                    }
                    GetWorldsInOrder(popularityResult.result.Popularity, new List<(WorldMeta, User)>(), result =>
                    {
                        foreach ((WorldMeta, User) tuple in result)
                        {
                            if (tuple.Item1 == null || tuple.Item2 == null) continue;
                            CreateWorldCardTemplate(tuple.Item1, tuple.Item2, PopularWorlds);
                        }
                    });
                    isGettingPopular = false;
                }));

        private void GetWorldsInOrder(Popularity[] popularities, List<(WorldMeta, User)> current,
            Action<List<(WorldMeta, User)>> onDone)
        {
            if(popularities.Length == current.Count)
                QuickInvoke.InvokeActionOnMainThread(onDone, current);
            else
            {
                Popularity popularity = popularities[current.Count];
                WorldTemplate.GetWorldMeta(popularity.Id, world =>
                {
                    if (world == null)
                    {
                        current.Add((null, null));
                        GetWorldsInOrder(popularities, current, onDone);
                        return;
                    }
                    APIPlayer.APIObject.GetUser(userResult =>
                    {
                        if (!userResult.success)
                        {
                            current.Add((null, null));
                            GetWorldsInOrder(popularities, current, onDone);
                            return;
                        }
                        current.Add((world, userResult.result.UserData));
                        GetWorldsInOrder(popularities, current, onDone);
                    }, world.OwnerId, isUserId: true);
                });
            }
        }
        
        private void CreateWorldCardTemplate(WorldMeta worldMeta, User creator, DynamicScroll scroll)
        {
            if(worldMeta == null || creator == null)
                return;
            GameObject worldCard = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("WorldCardTemplate").gameObject;
            GameObject newWorldCard = Instantiate(worldCard);
            RectTransform c = newWorldCard.GetComponent<RectTransform>();
            newWorldCard.GetComponent<WorldCardTemplate>().Render(WorldTemplate, WorldsPage, worldMeta, creator);
            scroll.AddItem(c);
            c.anchoredPosition = new Vector2(c.anchoredPosition.x, 0);
        }

        private void Start() => Refresh();
    }
}