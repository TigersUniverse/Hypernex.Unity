using System;
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
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UIActions
{
    public class WorldsPageManager : MonoBehaviour
    {
        public LoginPageTopBarButton WorldsPage;
        public WorldTemplate WorldTemplate;
        public DynamicScroll MyWorlds;
        public DynamicScroll FavoritedWorlds;
        public DynamicScroll WorldSearch;
        public TMP_InputField SearchField;
        public TMP_Dropdown SearchType;
        public Button NextPage;
        public Button PreviousPage;

        private bool isSearching;
        private int page;
        
        public void Search(int p = 0)
        {
            if (isSearching)
                return;
            WorldSearch.Clear();
            page = p;
            switch (SearchType.value)
            {
                case 0:
                    APIPlayer.APIObject.SearchByName(OnSearchResult, HypernexSharp.API.APIMessages.SearchType.World,
                        SearchField.text, page: p);
                    break;
                case 1:
                    APIPlayer.APIObject.SearchByName(OnSearchResult, HypernexSharp.API.APIMessages.SearchType.World,
                        SearchField.text, page: p);
                    break;
            }
        }

        public void Refresh()
        {
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
        
        private void CreateWorldCardTemplate(WorldMeta worldMeta, User creator, DynamicScroll scroll)
        {
            if(worldMeta == null || creator == null)
                return;
            GameObject worldCard = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("WorldCardTemplate").gameObject;
            GameObject newWorldCard = Instantiate(worldCard);
            RectTransform c = newWorldCard.GetComponent<RectTransform>();
            newWorldCard.GetComponent<WorldCardTemplate>().Render(WorldTemplate, WorldsPage, worldMeta, creator);
            scroll.AddItem(c);
            c.anchoredPosition = new Vector2(c.anchoredPosition.x, 0);
        }

        private void CreateWorldSearchTemplate(WorldMeta worldMeta)
        {
            if(worldMeta == null)
                return;
            GameObject worldSearch = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("WorldSearchTemplate").gameObject;
            GameObject newWorldSearch = Instantiate(worldSearch);
            RectTransform c = newWorldSearch.GetComponent<RectTransform>();
            newWorldSearch.GetComponent<WorldSearchTemplate>().Render(WorldTemplate, worldMeta);
            WorldSearch.AddItem(c);
        }

        private void OnSearchResult(CallbackResult<SearchResult> result) => QuickInvoke.InvokeActionOnMainThread(
            new Action(
                () =>
                {
                    isSearching = false;
                    if (!result.success) return;
                    foreach (string worldIds in result.result.Candidates)
                        WorldTemplate.GetWorldMeta(worldIds, meta =>
                        {
                            if (meta.Builds.Count(x => x.BuildPlatform == AssetBundleTools.Platform) > 0)
                                CreateWorldSearchTemplate(meta);
                        });
                }));

        private void Start()
        {
            NextPage.onClick.AddListener(() =>
            {
                page++;
                Search(page);
            });
            PreviousPage.onClick.AddListener(() =>
            {
                page--;
                Search(page);
            });
            Refresh();
        }
    }
}