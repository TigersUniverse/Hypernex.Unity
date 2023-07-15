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
using UnityEngine.Serialization;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UIActions
{
    public class AvatarsPageManager : MonoBehaviour
    {
        public LoginPageTopBarButton AvatarsPage;
        public AvatarTemplate AvatarTemplate;
        public DynamicScroll MyAvatars;
        public DynamicScroll FavoritedAvatars;
        public DynamicScroll AvatarSearch;
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
            AvatarSearch.Clear();
            page = p;
            switch (SearchType.value)
            {
                case 0:
                    APIPlayer.APIObject.SearchByName(OnSearchResult, HypernexSharp.API.APIMessages.SearchType.Avatar,
                        SearchField.text, page: p);
                    break;
                case 1:
                    APIPlayer.APIObject.SearchByName(OnSearchResult, HypernexSharp.API.APIMessages.SearchType.Avatar,
                        SearchField.text, page: p);
                    break;
            }
        }

        public void Refresh()
        {
            MyAvatars.Clear();
            FavoritedAvatars.Clear();
            foreach (string worldId in APIPlayer.APIUser.Avatars)
                AvatarTemplate.GetAvatarMeta(worldId, meta =>
                {
                    if (meta.Builds.Count(x => x.BuildPlatform == AssetBundleTools.Platform) > 0)
                        CreateAvatarCardTemplate(meta, APIPlayer.APIUser, MyAvatars);
                });
            foreach (string worldId in ConfigManager.SelectedConfigUser.SavedAvatars)
                AvatarTemplate.GetAvatarMeta(worldId, meta =>
                {
                    if (meta.Builds.Count(x => x.BuildPlatform == AssetBundleTools.Platform) > 0)
                        APIPlayer.APIObject.GetUser(
                            userResult => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                CreateAvatarCardTemplate(meta, userResult.result.UserData, FavoritedAvatars)
                            )), meta.OwnerId, isUserId: true);
                });
        }
        
        private void CreateAvatarCardTemplate(AvatarMeta avatarMeta, User creator, DynamicScroll scroll)
        {
            if(avatarMeta == null || creator == null)
                return;
            GameObject avatarCard = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("AvatarCardTemplate").gameObject;
            GameObject newAvatarCard = Instantiate(avatarCard);
            RectTransform c = newAvatarCard.GetComponent<RectTransform>();
            newAvatarCard.GetComponent<AvatarCardTemplate>().Render(AvatarTemplate, AvatarsPage, avatarMeta, creator);
            scroll.AddItem(c);
            c.anchoredPosition = new Vector2(c.anchoredPosition.x, 0);
        }

        private void CreateAvatarSearchTemplate(AvatarMeta avatarMeta)
        {
            if(avatarMeta == null)
                return;
            GameObject avatarSearch = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("AvatarSearchTemplate").gameObject;
            GameObject newAvatarSearch = Instantiate(avatarSearch);
            RectTransform c = newAvatarSearch.GetComponent<RectTransform>();
            newAvatarSearch.GetComponent<AvatarSearchTemplate>().Render(AvatarTemplate, avatarMeta);
            AvatarSearch.AddItem(c);
        }

        private void OnSearchResult(CallbackResult<SearchResult> result) => QuickInvoke.InvokeActionOnMainThread(
            new Action(
                () =>
                {
                    isSearching = false;
                    if (!result.success) return;
                    foreach (string avatarIds in result.result.Candidates)
                        AvatarTemplate.GetAvatarMeta(avatarIds, meta =>
                        {
                            if (meta.Builds.Count(x => x.BuildPlatform == AssetBundleTools.Platform) > 0)
                                CreateAvatarSearchTemplate(meta);
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
                if (page < 0)
                    page = 0;
                Search(page);
            });
            Refresh();
        }
    }
}