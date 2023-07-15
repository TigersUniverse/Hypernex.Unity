using System;
using UnityEngine;

namespace Hypernex.UIActions.Data
{
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
            CancelText = "CancelText";
            YesText = "Yes";
            NoText = "No";
            Result = OnSubmit ?? (_ => { });
            TimeToDisplay = t;
        }
    }
}