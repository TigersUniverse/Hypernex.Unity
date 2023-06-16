using System.Collections.Generic;
using Hypernex.Player;
using HypernexSharp.SocketObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UIActions
{
    public class TopBarManager : MonoBehaviour
    {
        public GameObject LoggedInObject;
        public TMP_Text WelcomeText;
        public Button SignoutButton;
        public TMP_InputField worldId;
        public Button joinWorldId;

        public readonly List<string> greetings = new()
            {"Howdy", "Hello", "Greetings", "Welcome", "G'day", "Hey", "Howdy-do", "Shalom"};

        private bool isLoggingOut;
    
        void Start()
        {
            APIPlayer.OnUser += user =>
            {
                int i = new System.Random().Next(greetings.Count);
                WelcomeText.text = greetings[i] + ", " + user.Username;
                LoggedInObject.SetActive(true);
            };
            APIPlayer.OnLogout += () => LoggedInObject.SetActive(false);
            SignoutButton.onClick.AddListener(() =>
            {
                if (!isLoggingOut)
                {
                    isLoggingOut = true;
                    APIPlayer.Logout(r => isLoggingOut = false);
                }
            });
            joinWorldId.onClick.AddListener(() =>
            {
                if (!APIPlayer.IsFullReady)
                    return;
                APIPlayer.APIObject.GetWorldMeta(result =>
                {
                    if (!result.success)
                        return;
                    APIPlayer.UserSocket.RequestNewInstance(result.result.Meta, InstancePublicity.Anyone, Init.Instance.InstanceProtocol);
                }, worldId.text);
            });
        }
    }
}
