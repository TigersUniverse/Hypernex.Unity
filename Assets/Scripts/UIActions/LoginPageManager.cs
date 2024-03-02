using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Player;
using Hypernex.UI;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.Tools;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UIActions
{
    public class LoginPageManager : MonoBehaviour
    {
        internal static List<SafeInstance> LastInstances = new();

        public LoginPageTopBarButton HomePage;
        public TMP_Text FriendsLabel;
        public Toggle ShowOfflineFriends;
        public DynamicScroll FriendsContainer;
        public TMP_Text FriendRequestsLabel;
        public DynamicScroll FriendRequestsContainer;
        public TMP_Text InstancesLabel;
        public DynamicScroll InstancesContainer;

        public ProfileTemplate ProfileTemplate;
        public WorldTemplate WorldTemplate;

        private List<User> lastFriends = new();

        private bool hasEnabledOnce;

        public void RefreshFriends()
        {
            if (APIPlayer.APIUser == null)
                return;
            //OnLogin(APIPlayer.APIUser);
            APIPlayer.RefreshUser();
        }

        public void RefreshInstances()
        {
            if (APIPlayer.APIObject == null)
                return;
            OnSocket();
        }

        public void ToggleOfflineFriends()
        {
            FriendsContainer.Clear();
            foreach (User lastFriend in lastFriends)
                if(lastFriend.Bio.Status != Status.Offline || ShowOfflineFriends.isOn)
                    CreateFriendCardFromUser(lastFriend);
        }
        
        private void OnEnable()
        {
            if(APIPlayer.APIUser == null)
                return;
            if (hasEnabledOnce) return;
            hasEnabledOnce = true;
            APIPlayer.OnUser += OnLogin;
            APIPlayer.OnUserRefresh += user =>
            {
                OnLogin(user);
            };
            APIPlayer.OnSocketConnect += OnSocket;
            APIPlayer.OnLogout += OnLogout;
            OnLogin(APIPlayer.APIUser);
        }

        private void CreateFriendCardFromUser(User user)
        {
            GameObject friendCard = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("FriendCardTemplate").gameObject;
            GameObject newFriendCard = Instantiate(friendCard);
            newFriendCard.GetComponent<FriendCardTemplate>().Render(this, user);
            FriendsContainer.AddItem(newFriendCard.GetComponent<RectTransform>());
        }

        private void CreateFriendRequestCardFromUser(User user)
        {
            GameObject friendRequestCard = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("FriendRequestCardTemplate").gameObject;
            GameObject newFriendRequestCard = Instantiate(friendRequestCard);
            RectTransform c = newFriendRequestCard.GetComponent<RectTransform>();
            newFriendRequestCard.GetComponent<FriendRequestCardTemplate>().Render(this, user,
                b =>
                {
                    FriendRequestsContainer.RemoveItem(c);
                    if(b)
                        CreateFriendCardFromUser(user);
                });
            FriendRequestsContainer.AddItem(c);
        }

        private void CreateInstanceCard(SafeInstance safeInstance, WorldMeta worldMeta, User host = null,
            User creator = null)
        {
            GameObject instanceCard = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("InstanceCardTemplate").gameObject;
            GameObject newInstanceCard = Instantiate(instanceCard);
            RectTransform c = newInstanceCard.GetComponent<RectTransform>();
            newInstanceCard.GetComponent<InstanceCardTemplate>()
                .Render(WorldTemplate, HomePage, safeInstance, worldMeta, host, creator);
            InstancesContainer.AddItem(c);
        }

        void OnLogin(User user)
        {
            FriendsLabel.text = "Friends (" + user.Friends.Count + ")";
            FriendsContainer.Clear();
            FriendRequestsContainer.Clear();
            lastFriends.Clear();
            foreach (string userFriendId in user.Friends)
            {
                APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    if (result.success)
                    {
                        if(lastFriends.Count(x => x.Id == result.result.UserData.Id) <= 0)
                            lastFriends.Add(result.result.UserData);
                        if(result.result.UserData.Bio.Status != Status.Offline || ShowOfflineFriends.isOn)
                            CreateFriendCardFromUser(result.result.UserData);
                    }
                    else
                        Logger.CurrentLogger.Error("Failed to get data for userid " + userFriendId + " for reason " +
                                                   result.message);
                })), userFriendId, null, true);
            }
            FriendRequestsLabel.text = "Friend Requests (" + user.FriendRequests.Count + ")";
            foreach (string userFriendRequestId in user.FriendRequests)
            {
                APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    if (result.success)
                        CreateFriendRequestCardFromUser(result.result.UserData);
                    else
                        Logger.CurrentLogger.Error("Failed to get data for userid " + userFriendRequestId + " for reason " +
                                                   result.message);
                })), userFriendRequestId, null, true);
            }
        }

        void OnSocket()
        {
            InstancesContainer.Clear();
            APIPlayer.GetAllSharedInstances(instances =>
            {
                int c = instances.Count;
                QuickInvoke.InvokeActionOnMainThread(new Action(() => LastInstances = new List<SafeInstance>(instances)));
                foreach (SafeInstance safeInstance in instances)
                {
                    WorldTemplate.GetWorldMeta(safeInstance.WorldId, meta =>
                    {
                        if (meta != null && meta.Builds.Count(x => x.BuildPlatform == AssetBundleTools.Platform) > 0)
                            APIPlayer.APIObject.GetUser(userResult =>
                            {
                                if (userResult.success)
                                    APIPlayer.APIObject.GetUser(creatorResult =>
                                    {
                                        if (creatorResult.success)
                                            QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                                CreateInstanceCard(safeInstance, meta,
                                                    userResult.result.UserData, creatorResult.result.UserData)));
                                        else
                                            c--;
                                    }, meta.OwnerId, isUserId: true);
                                else
                                    c--;
                            }, safeInstance.InstanceCreatorId, isUserId: true);
                        else
                            c--;
                    });
                }
                InstancesLabel.text = "Instances (" + c + ")";
            });
        }

        void OnLogout()
        {
            FriendsContainer.Clear();
            FriendRequestsContainer.Clear();
            InstancesContainer.Clear();
        }
    }
}
