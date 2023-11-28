using System;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.API;
using HypernexSharp.API.APIMessages;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Templates
{
    public class SearchTemplate : MonoBehaviour
    {
        public TMP_InputField SearchField;
        public TMP_Dropdown SearchByDropdown;
        public TMP_Dropdown SearchTypeDropdown;
        public DynamicScroll SearchList;

        private bool hasSearchedOnce = false;
        private bool isSearching;
        private int page;
        private string lastSearchTerm;
        
        public void Search(string overrideTerm = "")
        {
            if(isSearching)
                return;
            hasSearchedOnce = true;
            isSearching = true;
            SearchList.Clear();
            if (!string.IsNullOrEmpty(overrideTerm))
                SearchField.text = overrideTerm;
            else
                page = 0;
            switch (SearchTypeDropdown.value)
            {
                case 0:
                    APIPlayer.APIObject.SearchByName(OnSearchResult, SearchType.User, SearchField.text, page: page);
                    break;
                case 1:
                    switch (SearchByDropdown.value)
                    {
                        case 0:
                            APIPlayer.APIObject.SearchByName(OnSearchResult, SearchType.Avatar, SearchField.text,
                                page: page);
                            break;
                        case 1:
                            APIPlayer.APIObject.SearchByTag(OnSearchResult, SearchType.Avatar, SearchField.text,
                                page: page);
                            break;
                        default:
                            isSearching = false;
                            break;
                    }
                    break;
                case 2:
                    switch (SearchByDropdown.value)
                    {
                        case 0:
                            APIPlayer.APIObject.SearchByName(OnSearchResult, SearchType.World, SearchField.text,
                                page: page);
                            break;
                        case 1:
                            APIPlayer.APIObject.SearchByTag(OnSearchResult, SearchType.World, SearchField.text,
                                page: page);
                            break;
                        default:
                            isSearching = false;
                            break;
                    }
                    break;
                default:
                    isSearching = false;
                    break;
            }
            lastSearchTerm = SearchField.text;
        }

        public void PreviousPage()
        {
            if(!hasSearchedOnce || isSearching)
                return;
            page--;
            if (page < 0)
            {
                page = 0;
                return;
            }
            Search(lastSearchTerm);
        }
        
        public void NextPage()
        {
            if(!hasSearchedOnce || isSearching)
                return;
            page++;
            Search(lastSearchTerm);
        }

        private void OnSearchResult(CallbackResult<SearchResult> callbackResult) =>
            QuickInvoke.InvokeActionOnMainThread(new Action(
                () =>
                {
                    if (!callbackResult.success)
                    {
                        isSearching = false;
                        return;
                    }
                    foreach (string id in callbackResult.result.Candidates)
                    {
                        string s = id.Split('_')[0];
                        switch (s.ToLower())
                        {
                            case "user":
                                APIPlayer.APIObject.GetUser(userResult =>
                                {
                                    if(!userResult.success)
                                        return;
                                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                        CreateUserSearchCard(userResult.result.UserData)));
                                }, id, isUserId: true);
                                break;
                            case "avatar":
                                APIPlayer.APIObject.GetAvatarMeta(avatarMetaResult =>
                                {
                                    if(!avatarMetaResult.success)
                                        return;
                                    APIPlayer.APIObject.GetUser(userResult =>
                                    {
                                        if(!userResult.success)
                                            return;
                                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                            CreateAvatarSearchCard(avatarMetaResult.result.Meta,
                                                userResult.result.UserData)));
                                    }, avatarMetaResult.result.Meta.OwnerId, isUserId: true);
                                }, id);
                                break;
                            case "world":
                                WorldTemplate.GetWorldMeta(id, worldMeta =>
                                {
                                    if(worldMeta == null)
                                        return;
                                    APIPlayer.APIObject.GetUser(userResult =>
                                    {
                                        if(!userResult.success)
                                            return;
                                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                            CreateWorldSearchCard(worldMeta, userResult.result.UserData)));
                                    }, worldMeta.OwnerId, isUserId: true);
                                });
                                break;
                        }
                    }
                    isSearching = false;
                }));

        private SearchCardTemplate GetNewTemplate()
        {
            GameObject searchCard = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("SearchCardTemplate").gameObject;
            GameObject newSearchCard = Instantiate(searchCard);
            return newSearchCard.GetComponent<SearchCardTemplate>();
        }

        private void CreateUserSearchCard(User user)
        {
            SearchCardTemplate searchCardTemplate = GetNewTemplate();
            if(searchCardTemplate == null)
                return;
            searchCardTemplate.Render(user);
            RectTransform r = searchCardTemplate.gameObject.GetComponent<RectTransform>();
            SearchList.AddItem(r);
        }

        private void CreateAvatarSearchCard(AvatarMeta avatarMeta, User creator)
        {
            SearchCardTemplate searchCardTemplate = GetNewTemplate();
            if(searchCardTemplate == null)
                return;
            searchCardTemplate.Render(avatarMeta, creator);
            RectTransform r = searchCardTemplate.gameObject.GetComponent<RectTransform>();
            SearchList.AddItem(r);
        }

        private void CreateWorldSearchCard(WorldMeta worldMeta, User creator)
        {
            SearchCardTemplate searchCardTemplate = GetNewTemplate();
            if(searchCardTemplate == null)
                return;
            searchCardTemplate.Render(worldMeta, creator);
            RectTransform r = searchCardTemplate.gameObject.GetComponent<RectTransform>();
            SearchList.AddItem(r);
        }
    }
}