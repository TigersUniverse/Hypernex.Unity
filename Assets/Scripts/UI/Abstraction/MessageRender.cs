using System;
using Hypernex.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction
{
    public class MessageRender : UIRender, IRender<MessageMeta>
    {
        public RawImage LargeImage;
        public GameObject SmallImageContainer;
        public RawImage SmallImage;
        public TMP_Text Header;
        public TMP_Text SubHeader;
        public TMP_Text Description;
        public Button OK;
        public Button No;
        public Button Yes;
        public TMP_Text Time;
        
        public void Render(MessageMeta messageMeta)
        {
            if (messageMeta.LargeImage != null)
            {
                if (messageMeta.LargeImage.Value.Item1 != null)
                    LargeImage.texture = messageMeta.LargeImage.Value.Item1;
                else if(messageMeta.LargeImage.Value.Item2 != null)
                {
                    if (GifRenderer.IsGif(messageMeta.LargeImage.Value.Item2.Value.Item2))
                    {
                        GifRenderer gifRenderer = LargeImage.gameObject.GetComponent<GifRenderer>();
                        if (gifRenderer != null)
                            Destroy(gifRenderer);
                        gifRenderer = LargeImage.gameObject.AddComponent<GifRenderer>();
                        gifRenderer.LoadGif(messageMeta.LargeImage.Value.Item2.Value.Item2);
                    }
                    else
                        LargeImage.texture = ImageTools.BytesToTexture2D(messageMeta.LargeImage.Value.Item2.Value.Item1,
                            messageMeta.LargeImage.Value.Item2.Value.Item2);
                }
            }
            if (messageMeta.SmallImage != null)
            {
                if (messageMeta.SmallImage.Value.Item1 != null)
                    SmallImage.texture = messageMeta.SmallImage.Value.Item1;
                else if(messageMeta.SmallImage.Value.Item2 != null)
                {
                    if (GifRenderer.IsGif(messageMeta.SmallImage.Value.Item2.Value.Item2))
                    {
                        GifRenderer gifRenderer = SmallImage.gameObject.GetComponent<GifRenderer>();
                        if (gifRenderer != null)
                            Destroy(gifRenderer);
                        gifRenderer = SmallImage.gameObject.AddComponent<GifRenderer>();
                        gifRenderer.LoadGif(messageMeta.SmallImage.Value.Item2.Value.Item2);
                    }
                    else
                        SmallImage.texture = ImageTools.BytesToTexture2D(messageMeta.SmallImage.Value.Item2.Value.Item1,
                            messageMeta.SmallImage.Value.Item2.Value.Item2);
                }
                SmallImageContainer.SetActive(true);
            }
            else
                SmallImageContainer.SetActive(false);
            Header.text = messageMeta.Header;
            SubHeader.text = messageMeta.SubHeader;
            Description.text = messageMeta.Description;
            switch (messageMeta.Buttons)
            {
                case MessageButtons.None:
                    OK.gameObject.SetActive(false);
                    Yes.gameObject.SetActive(false);
                    No.gameObject.SetActive(false);
                    break;
                case MessageButtons.OK:
                    OK.gameObject.SetActive(true);
                    OK.transform.GetChild(0).GetComponent<TMP_Text>().text = messageMeta.OKText;
                    Yes.gameObject.SetActive(false);
                    No.gameObject.SetActive(false);
                    OK.onClick.RemoveAllListeners();
                    OK.onClick.AddListener(() => messageMeta.Result.Invoke(true));
                    break;
                case MessageButtons.YesNo:
                    OK.gameObject.SetActive(false);
                    Yes.gameObject.SetActive(true);
                    Yes.transform.GetChild(0).GetComponent<TMP_Text>().text = messageMeta.YesText;
                    No.gameObject.SetActive(true);
                    No.transform.GetChild(0).GetComponent<TMP_Text>().text = messageMeta.NoText;
                    Yes.onClick.RemoveAllListeners();
                    Yes.onClick.AddListener(() => messageMeta.Result.Invoke(true));
                    No.onClick.RemoveAllListeners();
                    No.onClick.AddListener(() => messageMeta.Result.Invoke(false));
                    break;
            }
            Time.text = TimeCounter.GetDate(messageMeta.Received) + " at " + TimeCounter.GetTime(messageMeta.Received);
        }
    }
    
    public struct MessageMeta
    {
        public DateTime Received { get; }
        public (Texture2D, (string, byte[])?)? LargeImage;
        public (Texture2D, (string, byte[])?)? SmallImage;
        public string Header;
        public string SubHeader;
        public string Description;
        public MessageUrgency MessageUrgency { get; }
        public MessageButtons Buttons { get; }
        public string OKText;
        public string CancelText;
        public string YesText;
        public string NoText;
        public Action<bool> Result;
        public float TimeToDisplay;

        public MessageMeta(MessageUrgency urgency, MessageButtons buttons, Action<bool> OnSubmit = null, float t = 3.0f)
        {
            Received = DateTime.Now;
            LargeImage = null;
            SmallImage = null;
            Header = String.Empty;
            SubHeader = String.Empty;
            Description = String.Empty;
            MessageUrgency = urgency;
            Buttons = buttons;
            OKText = "OK";
            CancelText = "Cancel";
            YesText = "Yes";
            NoText = "No";
            Result = OnSubmit ?? (_ => { });
            TimeToDisplay = t;
        }
    }
    
    public enum MessageButtons
    {
        None,
        OK,
        YesNo
    }
    
    public enum MessageUrgency
    {
        Info,
        Warning,
        Error
    }
}