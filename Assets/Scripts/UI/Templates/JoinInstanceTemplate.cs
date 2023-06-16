using System;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class JoinInstanceTemplate : MonoBehaviour
    {
        public TMP_Text WorldName;
        public TMP_Text WorldCreator;
        public TMP_Text InstanceHost;
        public TMP_Text InstanceVisibility;
        public DynamicScroll Users;
        public Button JoinButton;
        public Button ReturnButton;

        private SafeInstance Instance;
        private WorldMeta WorldMeta;

        private void CreateUserInstanceCard(User user)
        {
            GameObject instanceCard = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("UserInstanceCard").gameObject;
            GameObject newInstanceCard = Instantiate(instanceCard);
            RectTransform c = newInstanceCard.GetComponent<RectTransform>();
            newInstanceCard.GetComponent<UserInstanceCardTemplate>().Render(user);
            Users.AddItem(c);
        }

        public void Render(SafeInstance instance, WorldMeta worldMeta, User host, User creator)
        {
            Users.Clear();
            WorldName.text = worldMeta.Name;
            WorldCreator.text = "World By " + creator.Username;
            InstanceHost.text = "Hosted By " + host.Username;
            InstanceVisibility.text = instance.InstancePublicity.ToString();
            Instance = instance;
            WorldMeta = worldMeta;
            foreach (string userId in instance.ConnectedUsers)
                APIPlayer.APIObject.GetUser(result =>
                {
                    if (result.success)
                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                            CreateUserInstanceCard(result.result.UserData)));
                }, userId, isUserId: true);
        }

        private void Start()
        {
            JoinButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                SocketManager.JoinInstance(Instance);
                Users.Clear();
            });
            ReturnButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}