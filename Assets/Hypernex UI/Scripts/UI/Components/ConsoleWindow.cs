using System;
using System.Collections.Generic;
using System.Text;
using Hypernex.CCK;
using Hypernex.Game;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.UI.Abstraction;
using Nexport;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Components
{
    public class ConsoleWindow : MonoBehaviour
    {
        public static ConsoleWindow Instance { get; private set; }
        
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
        
        private List<(string, int)> ClientLogs = new ();
        private List<ServerConsoleLog> ServerLogs = new ();
        private int selected;
        public TMP_Text LogText;
        public GameObject ExecuteButton;
        public GameObject ServerButton;
        public TMP_InputField ScriptText;
        public TMP_Dropdown ScriptLanguageDropdown;
        
        public void AddMessage(string s, int logLevel = 0)
        {
            ClientLogs.Add((s, logLevel));
            Redraw();
        }

        public void AddMessage(ServerConsoleLog s)
        {
            ServerLogs.Add(s);
            Redraw();
        }
        
        public void SetClient()
        {
            selected = 0;
            Redraw();
        }
        
        public void SetServer()
        {
            if (!ExtendedPermissions)
            {
                if(selected > 0)
                {
                    selected = 0;
                    Redraw();
                }
                return;
            }
            selected = 1;
            Redraw();
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
            Redraw();
        }
        
        public void Execute()
        {
            if (GameInstance.FocusedInstance == null)
                return;
            if (GameInstance.FocusedInstance.worldMeta.OwnerId != APIPlayer.APIUser.Id)
                return;
            NexboxLanguage nexboxLanguage = (NexboxLanguage) ScriptLanguageDropdown.value;
            // TODO: OverlayManager
            switch (selected)
            {
                case 0:
                    NexboxScript script = new NexboxScript(nexboxLanguage, ScriptText.text){Name = "console"};
                    GameInstance.FocusedInstance.sandboxes.Add(new Sandbox(script, GameInstance.FocusedInstance,
                        GameInstance.FocusedInstance.World.gameObject));
                    OverlayNotification.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
                    {
                        Header = "Executed Script!",
                        Description = "Executed " + nexboxLanguage + " script on the client-side"
                    });
                    break;
                case 1:
                    ServerConsoleExecute serverConsoleExecute = new ServerConsoleExecute
                    {
                        Auth = new JoinAuth
                        {
                            UserId = APIPlayer.APIUser.Id,
                            TempToken = GameInstance.FocusedInstance.userIdToken
                        },
                        Language = nexboxLanguage,
                        ScriptText = ScriptText.text
                    };
                    GameInstance.FocusedInstance.SendMessage(typeof(ServerConsoleExecute).FullName,
                        Msg.Serialize(serverConsoleExecute));
                    OverlayNotification.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
                    {
                        Header = "Executed Script!",
                        Description = "Executed " + nexboxLanguage + " script on the server-side"
                    });
                    break;
            }
        }

        private string GetEmoji(int level)
        {
            string beginEmoji;
            switch (level)
            {
                case 0:
                    beginEmoji = "ℹ️";
                    break;
                case 1:
                    beginEmoji = "⚠️";
                    break;
                default:
                    beginEmoji = "⛔";
                    break;
            }
            return beginEmoji;
        }
        
        private void Redraw()
        {
            if (!gameObject.activeInHierarchy) return;
            StringBuilder completeLog = new StringBuilder();
            int x = 0;
            switch (selected)
            {
                case 0:
                    foreach ((string, int) clientLog in ClientLogs)
                    {
                        completeLog.Append(GetEmoji(clientLog.Item2));
                        completeLog.Append(' ');
                        completeLog.Append(clientLog.Item1);
                        if (x < ClientLogs.Count)
                            completeLog.Append('\n');
                        x++;
                    }
                    break;
                default:
                    foreach (ServerConsoleLog serverLog in ServerLogs)
                    {
                        completeLog.Append(GetEmoji(serverLog.LogLevel));
                        completeLog.Append($" [{serverLog.ScriptName}] {serverLog.Log}");
                        if (x < ClientLogs.Count)
                            completeLog.Append('\n');
                        x++;
                    }
                    break;
            }
            LogText.text = completeLog.ToString();
        }

        internal void Initialize()
        {
            if (Instance != null)
            {
                Destroy(this);
                throw new Exception("Cannot have multiple instances of ConsoleWindow!");
            }
            Instance = this;
        }

        private void OnEnable() => Redraw();
        
        private void Update()
        {
            if (GameInstance.FocusedInstance == null)
            {
                ExecuteButton.SetActive(false);
                ServerButton.SetActive(false);
                return;
            }
            if (GameInstance.FocusedInstance.worldMeta.OwnerId != APIPlayer.APIUser.Id)
            {
                ExecuteButton.SetActive(false);
                ServerButton.SetActive(false);
                return;
            }
            ExecuteButton.SetActive(ExtendedPermissions);
            ServerButton.SetActive(ExtendedPermissions);
        }
    }
}