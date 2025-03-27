using System;
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
using UnityEngine.UI;

namespace Hypernex.UI.Pages
{
    public class SearchPage : UIPage
    {
        private const int MAX_RESULTS = 50;
        
        public TMP_InputField SearchField;
        public ScrollRect SearchScroll;
        public RectTransform SearchList;
        public ToggleButton[] CategoryToggles;
        public GameObject[] PresetTags;
        
        private bool isSearching;
        private int page;
        private int lastResultsLength;
        private string lastSearch = String.Empty;
        private int lastType = -1;
        private SearchType lastSearchIndex;
        
        private int SelectedIndex
        {
            get
            {
                for (int i = 0; i < CategoryToggles.Length; i++)
                {
                    ToggleButton t = CategoryToggles[i];
                    if(!t.isOn) continue;
                    return i;
                }
                return -1;
            }
        }
        
        public void SearchName()
        {
            if(isSearching) return;
            int index = SelectedIndex;
            if(index < 0) return;
            string n = SearchField.text;
            if(string.IsNullOrEmpty(n) || n.Length < 3) return;
            ResetSearch();
            Search(0, (SearchType) index, n);
        }
        
        public void SearchTag()
        {
            if(isSearching) return;
            int index = SelectedIndex;
            if(index < 0) return;
            string n = SearchField.text;
            if(string.IsNullOrEmpty(n) || n.Length < 3) return;
            ResetSearch();
            Search(1, (SearchType) index, n);
        }

        public void SearchName(string n)
        {
            if(isSearching) return;
            int index = SelectedIndex;
            if(index < 0) return;
            if(string.IsNullOrEmpty(n) || n.Length < 3) return;
            ResetSearch();
            Search(0, (SearchType) index, n);
        }
        
        public void SearchTag(string n)
        {
            if(isSearching) return;
            int index = SelectedIndex;
            if(index < 0) return;
            if(string.IsNullOrEmpty(n) || n.Length < 3) return;
            ResetSearch();
            Search(1, (SearchType) index, n);
        }

        private void ResetSearch()
        {
            page = 0;
            lastSearch = String.Empty;
            lastType = -1;
            SearchList.ClearChildren();
        }

        private void Search(int type, SearchType index, string s)
        {
            switch (index)
            {
                case SearchType.User:
                    APIPlayer.APIObject.SearchByName(OnSearchResult, SearchType.User, s, MAX_RESULTS, page);
                    break;
                case SearchType.Avatar:
                    switch (type)
                    {
                        case 0:
                            APIPlayer.APIObject.SearchByName(OnSearchResult, SearchType.Avatar, s,
                                MAX_RESULTS, page);
                            break;
                        case 1:
                            APIPlayer.APIObject.SearchByTag(OnSearchResult, SearchType.Avatar, s,
                                MAX_RESULTS, page);
                            break;
                        default:
                            isSearching = false;
                            break;
                    }
                    break;
                case SearchType.World:
                    switch (type)
                    {
                        case 0:
                            APIPlayer.APIObject.SearchByName(OnSearchResult, SearchType.World, s,
                                MAX_RESULTS, page);
                            break;
                        case 1:
                            APIPlayer.APIObject.SearchByTag(OnSearchResult, SearchType.World, s,
                                MAX_RESULTS, page);
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
            isSearching = true;
            lastSearch = s;
            lastType = type;
            lastSearchIndex = index;
        }
        
        private void OnSearchResult(CallbackResult<SearchResult> callbackResult) =>
            QuickInvoke.InvokeActionOnMainThread(new Action(
                () =>
                {
                    try
                    {
                        if (!callbackResult.success) return;
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
                                    AvatarRender.GetAvatarMeta(id, avatarMeta =>
                                    {
                                        if (avatarMeta == null)
                                            return;
                                        CreateAvatarSearchCard(avatarMeta);
                                    });
                                    break;
                                case "world":
                                    WorldRender.GetWorldMeta(id, worldMeta =>
                                    {
                                        if(worldMeta == null)
                                            return;
                                        CreateWorldSearchCard(worldMeta);
                                    });
                                    break;
                            }
                        }
                    }
                    finally
                    {
                        if (callbackResult.success && callbackResult.result != null &&
                            callbackResult.result.Candidates != null)
                            lastResultsLength = callbackResult.result.Candidates.Count;
                        else
                            lastResultsLength = 0;
                        isSearching = false;
                    }
                }));
        
        private void CreateUserSearchCard(User user)
        {
            IRender<User> userRenderer = Defaults.GetRenderer<User>("FriendCardTemplate");
            userRenderer.Render(user);
            SearchList.AddChild(userRenderer.transform);
        }

        private void CreateAvatarSearchCard(AvatarMeta avatarMeta)
        {
            IRender<AvatarMeta> avatarRender = Defaults.GetRenderer<AvatarMeta>("AvatarCardTemplate");
            avatarRender.Render(avatarMeta);
            SearchList.AddChild(avatarRender.transform);
        }

        private void CreateWorldSearchCard(WorldMeta worldMeta)
        {
            IRender<WorldMeta> worldRender = Defaults.GetRenderer<WorldMeta>("WorldCardTemplate");
            worldRender.Render(worldMeta);
            SearchList.AddChild(worldRender.transform);
        }

        public void OnToggleChanged()
        {
            int index = SelectedIndex;
            if(index < 0) return;
            foreach (GameObject presetTag in PresetTags)
                presetTag.SetActive(false);
            PresetTags[index].SetActive(true);
            string inp = "User Search...";
            switch (index)
            {
                case 1:
                    inp = "Avatar Search...";
                    break;
                case 2:
                    inp = "World Search...";
                    break;
            }
            ((TMP_Text) SearchField.placeholder).text = inp;
        }

        private void Update()
        {
            if (isSearching || lastResultsLength < MAX_RESULTS || lastType < 0 || string.IsNullOrEmpty(lastSearch)) return;
            Vector2 pos = SearchScroll.normalizedPosition;
            if(pos.y > 0) return;
            page++;
            Search(lastType, lastSearchIndex, lastSearch);
        }
    }
}