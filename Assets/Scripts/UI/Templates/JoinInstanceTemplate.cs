using System;
using Hypernex.Game;
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

        private void CreateUserInstanceCard(User user, GameObject overlay)
        {
            GameObject instanceCard = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("UserInstanceCard").gameObject;
            GameObject newInstanceCard = Instantiate(instanceCard);
            RectTransform c = newInstanceCard.GetComponent<RectTransform>();
            newInstanceCard.GetComponent<UserInstanceCardTemplate>().Render(user, overlay);
            Users.AddItem(c);
        }

        public void Render(SafeInstance instance, WorldMeta worldMeta, User host, User creator, GameObject overlay)
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
                            CreateUserInstanceCard(result.result.UserData, overlay)));
                }, userId, isUserId: true);
        }

        private void Start()
        {
            JoinButton.onClick.AddListener(() =>
            {
                if (GameInstance.FocusedInstance != null &&
                    GameInstance.FocusedInstance.gameServerId == Instance.GameServerId &&
                    GameInstance.FocusedInstance.instanceId == Instance.InstanceId)
                    return;
                gameObject.SetActive(false);
                SocketManager.JoinInstance(Instance);
                Users.Clear();
            });
            ReturnButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}