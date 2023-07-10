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
        public List<GameObject> LoggedInObjects;
        public TMP_Text WelcomeText;
        public Button SignoutButton;

        public readonly List<string> greetings = new()
            {"Howdy", "Hello", "Greetings", "Welcome", "G'day", "Hey", "Howdy-do", "Shalom"};

        private bool isLoggingOut;
    
        void Start()
        {
            APIPlayer.OnUser += user =>
            {
                int i = new System.Random().Next(greetings.Count);
                WelcomeText.text = greetings[i] + ", " + user.Username;
                LoggedInObjects.ForEach(x => x.SetActive(true));
            };
            APIPlayer.OnLogout += () => LoggedInObjects.ForEach(x => x.SetActive(false));
            SignoutButton.onClick.AddListener(() =>
            {
                if (!isLoggingOut)
                {
                    isLoggingOut = true;
                    APIPlayer.Logout(r => isLoggingOut = false);
                }
            });
        }
    }
}
