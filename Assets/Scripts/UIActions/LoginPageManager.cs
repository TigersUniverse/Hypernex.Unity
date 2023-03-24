using System;
using Hypernex.Player;
using Hypernex.UI;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.Tools;
using UnityEngine;
using Logger = Hypernex.Logging.Logger;

namespace Hypernex.UIActions
{
    public class LoginPageManager : MonoBehaviour
    {
        public TMP_Text FriendsLabel;
        public DynamicScroll FriendsContainer;
        public TMP_Text FriendRequestsLabel;
        public DynamicScroll FriendRequestsContainer;

        public ProfileTemplate ProfileTemplate;

        private bool hasEnabledOnce;
        
        void OnEnable()
        {
            if(APIPlayer.APIUser == null)
                return;
            APIPlayer.OnUser += OnLogin;
            APIPlayer.OnUserRefresh += user =>
            {
                FriendsContainer.Clear();
                OnLogin(user);
            };
            APIPlayer.OnLogout += OnLogout;
            if (!hasEnabledOnce)
            {
                hasEnabledOnce = true;
                OnLogin(APIPlayer.APIUser);
            }
        }

        private void CreateFriendCardFromUser(User user)
        {
            GameObject friendCard = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("FriendCardTemplate").gameObject;
            GameObject newFriendCard = Instantiate(friendCard);
            newFriendCard.GetComponent<FriendCardTemplate>().Render(this, user);
            FriendsContainer.AddItem(newFriendCard.GetComponent<RectTransform>());
        }

        private void CreateFriendRequestCardFromUser(User user)
        {
            GameObject friendRequestCard = DontDestroyMe.GetNotDestroyedObject("Templates").transform
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

        void OnLogin(User user)
        {
            FriendsLabel.text = "Friends (" + user.Friends.Count + ")";
            foreach (string userFriendId in user.Friends)
            {
                APIPlayer.APIObject.GetUser(result => QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    if (result.success)
                        CreateFriendCardFromUser(result.result.UserData);
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

        void OnLogout()
        {
            FriendsContainer.Clear();
            FriendRequestsContainer.Clear();
        }
    }
}
