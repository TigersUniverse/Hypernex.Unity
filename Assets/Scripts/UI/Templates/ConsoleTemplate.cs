using System.Collections.Generic;
using Hypernex.Game;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Templates
{
    public class ConsoleTemplate : MonoBehaviour
    {
        public static ConsoleTemplate Instance { get; private set; }

        public static bool ExtendedPermissions
        {
            get
            {
                if (APIPlayer.APIUser == null || GameInstance.FocusedInstance == null)
                    return false;
                if (APIPlayer.APIUser.Id == GameInstance.FocusedInstance.worldMeta.OwnerId)
                    return true;
                return GameInstance.FocusedInstance.instanceCreatorId ==
                    GameInstance.FocusedInstance.worldMeta.OwnerId && GameInstance.FocusedInstance.IsModerator;
            }
        }

        public DynamicScroll Scroll;
        public GameObject ServerButton;
        public Sprite Warning;
        public Sprite Error;
        
        private static List<(string, int)> ClientLogs = new ();
        private static List<ServerConsoleLog> ServerLogs = new ();

        private static int selected;

        private void CloneTemplate(string text, int logLevel)
        {
            GameObject g = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("ConsoleTemplate").gameObject;
            GameObject newG = Instantiate(g);
            RectTransform c = newG.GetComponent<RectTransform>();
            newG.transform.GetChild(1).GetComponent<TMP_Text>().text = text;
            Image img = newG.transform.GetChild(0).GetComponent<Image>();
            switch (logLevel)
            {
                case 1:
                    img.sprite = Warning;
                    break;
                case 2:
                    img.sprite = Error;
                    break;
            }
            Scroll.AddItem(c);
        }

        public static void AddMessage(string s, int logLevel = 0)
        {
            if(selected == 0 && Instance != null && Instance.gameObject.activeSelf)
                Instance.CloneTemplate(s, logLevel);
            ClientLogs.Add((s, logLevel));
        }

        public static void AddMessage(ServerConsoleLog s)
        {
            if(selected == 1 && Instance != null && Instance.gameObject.activeSelf)
                Instance.CloneTemplate($"[{s.ScriptName}] {s.Log}", s.LogLevel);
            ServerLogs.Add(s);
        }

        public void SetClient()
        {
            selected = 0;
            if(Instance != null)
                Instance.ResetMessages();
        }
        
        public void SetServer()
        {
            if (!ExtendedPermissions)
                return;
            selected = 1;
            if(Instance != null)
                Instance.ResetMessages();
        }

        public void Clear()
        {
            switch (selected)
            {
                case 0:
                    ClientLogs.Clear();
                    break;
                case 1:
                    ServerLogs.Clear();
                    break;
            }

            ResetMessages();
        }

        private void ResetMessages()
        {
            if (!gameObject.activeSelf)
                return;
            if (!Scroll.enabled || !Scroll.gameObject.activeSelf)
            {
                Scroll.gameObject.SetActive(true);
                Scroll.enabled = true;
            }
            Scroll.Clear();
            if(selected == 0)
                foreach ((string, int ) s in ClientLogs)
                    CloneTemplate(s.Item1, s.Item2);
            else
                foreach (ServerConsoleLog s in ServerLogs)
                    CloneTemplate($"[{s.ScriptName}] {s.Log}", s.LogLevel);
        }

        private void OnEnable()
        {
            Instance = this;
            ResetMessages();
        }

        private void Update() => ServerButton.SetActive(ExtendedPermissions);
    }
}