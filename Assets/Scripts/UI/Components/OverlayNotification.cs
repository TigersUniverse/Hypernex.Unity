using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game;
using Hypernex.UI.Abstraction;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Components
{
    public class OverlayNotification : MonoBehaviour, IDisposable
    {
        public static OverlayNotification Instance { get; set; }

        public static void AddMessageToQueue(MessageMeta messageMeta) =>
            Instance.MessagesToDisplay.Enqueue(messageMeta);
        
        public static void ClearNonImportantMessages()
        {
            List<MessageMeta> one = new List<MessageMeta>(Instance.MessagesToDisplay);
            Instance.MessagesToDisplay.Clear();
            for (int i = 0; i < one.Count; i++)
            {
                MessageMeta meta = one.ElementAt(i);
                if(meta.MessageUrgency == MessageUrgency.Info) continue;
                Instance.MessagesToDisplay.Enqueue(meta);
            }
        }

        public LocalPlayer LocalPlayer;
        public RawImage MicrophoneIcon;
        public UIThemeObject MicTheme;
        public Texture2D MicUnmute;
        public Texture2D MicMute;
        public List<NotificationRender> Panels = new();
        public Transform OverlayAlign;
        public Transform OverlayVRAlign;
        public Slider DownloadProgress;
        public TMP_Text DownloadName;
        public TMP_Text DownloadProgressText;

        internal AvatarMeta CurrentLoadingAvatarMeta;
        private CancellationTokenSource cts;
        private Coroutine coroutine;
        private readonly Queue<MessageMeta> MessagesToDisplay = new();
        private bool isShowingMessage;

        private List<NotificationRender> GetMessagePanelTemplate(MessageUrgency messageUrgency) =>
            Panels.Where(x => x.UrgencyPanel == messageUrgency).ToList();

        internal void Begin()
        {
            Instance = this;
            cts = new CancellationTokenSource();
            coroutine = StartCoroutine(MessageShowLoop());
            UnityLogger.OnLog += o => Defaults.Instance.Console.AddMessage($"[GAME] {o}");
            UnityLogger.OnWarn += o => Defaults.Instance.Console.AddMessage($"[GAME] {o}", 1);
            UnityLogger.OnError += o => Defaults.Instance.Console.AddMessage($"[GAME] {o}", 2);
            UnityLogger.OnCritical += o => Defaults.Instance.Console.AddMessage($"[CRITICAL-GAME] {o}", 2);
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
                    List<NotificationRender> messagePanelTemplates = GetMessagePanelTemplate(messageMeta.MessageUrgency);
                    messagePanelTemplates.ForEach(x => x.Render((messageMeta.Header, messageMeta.Description)));
                    yield return new WaitForSeconds(messageMeta.TimeToDisplay);
                    messagePanelTemplates.ForEach(x => x.Hide());
                    yield return new WaitForSeconds(1f);
                    isShowingMessage = false;
                }
            }
        }

        private void CheckForDownloading()
        {
            (WorldMeta, float)[] e = GameInstance.GetAllDownloads();
            float[] playerClone;
            if (LocalPlayer.IsLoadingAvatar)
            {
                playerClone = new float[e.Length + 1];
                for (int i = 0; i < e.Length; i++)
                    playerClone[i] = e[i].Item2;
                playerClone[playerClone.Length - 1] = LocalPlayer.AvatarDownloadPercentage;
            }
            else
                playerClone = e.Select(x => x.Item2).ToArray();
            if (playerClone.Length <= 0)
            {
                DownloadProgress.gameObject.SetActive(false);
                return;
            }
            DownloadProgress.gameObject.SetActive(true);
            float avg = playerClone.Sum();
            avg /= playerClone.Length;
            DownloadProgress.value = avg;
            DownloadProgressText.text = avg.ToString("P0", CultureInfo.CurrentCulture);
            string assetName;
            if (playerClone.Length == 1)
                assetName = e.Length > 0 ? e[0].Item1.Name : CurrentLoadingAvatarMeta.Name;
            else
                assetName = $"{playerClone.Length} assets";
            DownloadName.text = "Downloading " + assetName;
        }

        private void Update()
        {
            transform.localPosition = LocalPlayer.IsVR ? OverlayVRAlign.localPosition : OverlayAlign.localPosition;
            MicrophoneIcon.texture = LocalPlayer.MicrophoneEnabled ? MicUnmute : MicMute;
            MicTheme.ApplyTheme(UITheme.SelectedTheme);
            CheckForDownloading();
        }

        public void Dispose()
        {
            cts?.Dispose();
            StopCoroutine(coroutine);
        }
    }
}