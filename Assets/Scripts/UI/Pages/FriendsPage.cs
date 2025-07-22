using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Abstraction;
using Hypernex.UI.Components;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Pages
{
    public class FriendsPage : UIPage
    {
        public RectTransform FriendsContainer;
        public TMP_Text FriendsLabel;
        public ToggleButton[] CategoryToggles;
        public Toggle ShowOfflineFriends;
        
        private void RenderPage()
        {
            FriendsContainer.ClearChildren();
            User user = APIPlayer.APIUser;
            switch (CategoryToggles.GetSelectedIndex())
            {
                case 0:
                    FriendsLabel.text = "Friends (" + user.Friends.Count + ")";
                    foreach (string userFriendId in user.Friends)
                    {
                        APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        {
                            if(CategoryToggles.GetSelectedIndex() != 0) return;
                            if (result.success)
                            {
                                if(UserCardExists(result.result.UserData)) return;
                                if(result.result.UserData.Bio.Status != Status.Offline || ShowOfflineFriends.isOn)
                                    CreateFriendCardFromUser(result.result.UserData);
                            }
                            else
                                Logger.CurrentLogger.Error("Failed to get data for userid " + userFriendId + " for reason " +
                                                           result.message);
                        })), userFriendId, null, true);
                    }
                    break;
                case 2:
                    FriendsLabel.text = "Friend Requests (" + user.FriendRequests.Count + ")";
                    foreach (string userFriendRequestId in user.FriendRequests)
                    {
                        APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        {
                            if(CategoryToggles.GetSelectedIndex() != 2) return;
                            if (result.success)
                                CreateFriendRequestCardFromUser(result.result.UserData);
                            else
                                Logger.CurrentLogger.Error("Failed to get data for userid " + userFriendRequestId + " for reason " +
                                                           result.message);
                        })), userFriendRequestId, null, true);
                    }
                    break;
                case 1:
                    FriendsLabel.text = "Followed Users (" + user.Following.Count + ")";
                    foreach (string userFollowingId in user.Following)
                    {
                        APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        {
                            if(CategoryToggles.GetSelectedIndex() != 1) return;
                            if (result.success)
                            {
                                if(UserCardExists(result.result.UserData)) return;
                                if(result.result.UserData.Bio.Status != Status.Offline || ShowOfflineFriends.isOn)
                                    CreateFriendCardFromUser(result.result.UserData);
                            }
                            else
                                Logger.CurrentLogger.Error("Failed to get data for userid " + userFollowingId + " for reason " +
                                                           result.message);
                        })), userFollowingId, null, true);
                    }
                    break;
            }
        }

        private bool UserCardExists(User user)
        {
            for (int i = 0; i < FriendsContainer.childCount; i++)
            {
                Transform t = FriendsContainer.GetChild(i);
                UserRender userRender = t.GetComponent<UserRender>();
                if(userRender == null) continue;
                if (userRender.u.Id == user.Id) return true;
            }
            return false;
        }
        
        private void CreateFriendCardFromUser(User user)
        {
            IRender<User> userRenderer = Defaults.GetRenderer<User>("FriendCardTemplate");
            userRenderer.Render(user);
            FriendsContainer.AddChild(userRenderer.transform);
        }
        
        private void CreateFriendRequestCardFromUser(User user)
        {
            IRender<User> userRenderer = Defaults.GetRenderer<User>("FriendRequestCardTemplate");
            userRenderer.Render(user);
            FriendsContainer.AddChild(userRenderer.transform);
        }

        public void OnChangeIndex() => RenderPage();
        
        private void OnLogout()
        {
            FriendsContainer.ClearChildren();
        }

        public override void Show(bool hideAll = true)
        {
            base.Show(hideAll);
            RenderPage();
        }

        internal override void Initialize()
        {
            if(!HasInitialized) APIPlayer.OnLogout += OnLogout;
            base.Initialize();
        }
    }
}