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
    public class AvatarsPageManager : MonoBehaviour
    {
        public LoginPageTopBarButton AvatarsPage;
        public AvatarTemplate AvatarTemplate;
        public TMP_Dropdown PopularityTypeDropdown;
        public DynamicScroll PopularAvatars;
        public DynamicScroll MyAvatars;
        public DynamicScroll FavoritedAvatars;
        
        private bool isGettingPopular;

        public void Refresh()
        {
            if(!isGettingPopular)
                RefreshPopularAvatars();
            MyAvatars.Clear();
            FavoritedAvatars.Clear();
            foreach (string avatarId in APIPlayer.APIUser.Avatars)
                AvatarTemplate.GetAvatarMeta(avatarId, meta =>
                {
                    if (meta.Builds.Count(x => x.BuildPlatform == AssetBundleTools.Platform) > 0)
                        CreateAvatarCardTemplate(meta, APIPlayer.APIUser, MyAvatars);
                });
            foreach (string avatarId in ConfigManager.SelectedConfigUser.SavedAvatars)
                AvatarTemplate.GetAvatarMeta(avatarId, meta =>
                {
                    if (meta.Builds.Count(x => x.BuildPlatform == AssetBundleTools.Platform) > 0)
                        APIPlayer.APIObject.GetUser(
                            userResult => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                CreateAvatarCardTemplate(meta, userResult.result.UserData, FavoritedAvatars)
                            )), meta.OwnerId, isUserId: true);
                });
        }
        
        public void RefreshPopularAvatars()
        {
            if(isGettingPopular)
                return;
            isGettingPopular = true;
            PopularAvatars.Clear();
            PopularityType popularityType = (PopularityType) PopularityTypeDropdown.value;
            APIPlayer.APIObject.GetAvatarPopularity(OnPopularityResult, popularityType);
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
                    GetAvatarsInOrder(popularityResult.result.Popularity, new List<(AvatarMeta, User)>(), result =>
                    {
                        foreach ((AvatarMeta, User) tuple in result)
                        {
                            if (tuple.Item1 == null || tuple.Item2 == null) continue;
                            CreateAvatarCardTemplate(tuple.Item1, tuple.Item2, PopularAvatars);
                        }
                    });
                    isGettingPopular = false;
                }));

        private void GetAvatarsInOrder(Popularity[] popularities, List<(AvatarMeta, User)> current,
            Action<List<(AvatarMeta, User)>> onDone)
        {
            if(popularities.Length == current.Count)
                QuickInvoke.InvokeActionOnMainThread(onDone, current);
            else
            {
                Popularity popularity = popularities[current.Count];
                APIPlayer.APIObject.GetAvatarMeta(avatarResult =>
                {
                    if (!avatarResult.success)
                    {
                        current.Add((null, null));
                        GetAvatarsInOrder(popularities, current, onDone);
                        return;
                    }
                    APIPlayer.APIObject.GetUser(userResult =>
                    {
                        if (!userResult.success)
                        {
                            current.Add((null, null));
                            GetAvatarsInOrder(popularities, current, onDone);
                            return;
                        }
                        current.Add((avatarResult.result.Meta, userResult.result.UserData));
                        GetAvatarsInOrder(popularities, current, onDone);
                    }, avatarResult.result.Meta.OwnerId, isUserId: true);
                }, popularity.Id);
            }
        }
        
        private void CreateAvatarCardTemplate(AvatarMeta avatarMeta, User creator, DynamicScroll scroll)
        {
            if(avatarMeta == null || creator == null)
                return;
            GameObject avatarCard = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("AvatarCardTemplate").gameObject;
            GameObject newAvatarCard = Instantiate(avatarCard);
            RectTransform c = newAvatarCard.GetComponent<RectTransform>();
            newAvatarCard.GetComponent<AvatarCardTemplate>().Render(AvatarTemplate, AvatarsPage, avatarMeta, creator);
            scroll.AddItem(c);
            c.anchoredPosition = new Vector2(c.anchoredPosition.x, 0);
        }

        private void Start() => Refresh();
    }
}