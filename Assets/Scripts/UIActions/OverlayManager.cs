using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hypernex.CCK.Unity;
using Hypernex.Game;
using Hypernex.UI.Templates;
using Hypernex.UIActions.Data;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UIActions
{
    public class OverlayManager : MonoBehaviour, IDisposable
    {
        private static OverlayManager Instance { get; set; }

        public static void AddMessageToQueue(MessageMeta messageMeta) =>
            Instance.MessagesToDisplay.Enqueue(messageMeta);

        public LocalPlayer LocalPlayer;
        public GameObject MicrophoneIcon;
        public List<MessagePanelTemplate> Panels = new();
        public Transform OverlayAlign;
        public Transform OverlayVRAlign;

        private CancellationTokenSource cts;
        private Coroutine coroutine;
        private readonly Queue<MessageMeta> MessagesToDisplay = new();
        private bool isShowingMessage;

        private List<MessagePanelTemplate> GetMessagePanelTemplate(MessageUrgency messageUrgency) =>
            Panels.Where(x => x.UrgencyPanel == messageUrgency).ToList();

        internal void Begin()
        {
            Instance = this;
            cts = new CancellationTokenSource();
            coroutine = StartCoroutine(MessageShowLoop());
            /*UnityLogger.OnLog += o => AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
            {
                Header = "Info",
                Description = o.ToString()
            });
            UnityLogger.OnWarn += o => AddMessageToQueue(new MessageMeta(MessageUrgency.Warning, MessageButtons.None)
            {
                Header = "Warning",
                Description = o.ToString()
            });
            UnityLogger.OnError += o => AddMessageToQueue(new MessageMeta(MessageUrgency.Error, MessageButtons.None)
            {
                Header = "Error",
                Description = o.ToString()
            });
            UnityLogger.OnCritical += o => AddMessageToQueue(new MessageMeta(MessageUrgency.Error, MessageButtons.None)
            {
                Header = "Exception Raised",
                Description = o.ToString()
            });*/
            UnityLogger.OnLog += o => ConsoleTemplate.AddMessage($"[GAME] {o}");
            UnityLogger.OnWarn += o => ConsoleTemplate.AddMessage($"[GAME] {o}", 1);
            UnityLogger.OnError += o => ConsoleTemplate.AddMessage($"[GAME] {o}", 2);
            UnityLogger.OnCritical += o => ConsoleTemplate.AddMessage($"[CRITICAL-GAME] {o}", 2);
        }

        private IEnumerator MessageShowLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                if (MessagesToDisplay.Count <= 0 || isShowingMessage)
                    yield return null;
                else
                {
                    isShowingMessage = true;
                    MessageMeta messageMeta = MessagesToDisplay.Dequeue();
                    List<MessagePanelTemplate> messagePanelTemplates = GetMessagePanelTemplate(messageMeta.MessageUrgency);
                    messagePanelTemplates.ForEach(x => x.Render(messageMeta.Header, messageMeta.Description));
                    yield return new WaitForSeconds(messageMeta.TimeToDisplay);
                    messagePanelTemplates.ForEach(x => x.Hide());
                    yield return new WaitForSeconds(1f);
                    isShowingMessage = false;
                }
            }
        }

        private void Update()
        {
            transform.localPosition = LocalPlayer.IsVR ? OverlayVRAlign.localPosition : OverlayAlign.localPosition;
            MicrophoneIcon.SetActive(LocalPlayer.MicrophoneEnabled);
        }

        public void Dispose()
        {
            cts?.Dispose();
            StopCoroutine(coroutine);
        }
    }
}