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
        public MessageButtons Buttons { get; }
        public string OKText;
        public string CancelText;
        public string YesText;
        public string NoText;
        public Action<bool> Result;

        public MessageMeta(MessageButtons buttons, Action<bool> OnSubmit = null)
        {
            Received = DateTime.Now;
            LargeImage = null;
            SmallImage = null;
            Header = String.Empty;
            SubHeader = String.Empty;
            Description = String.Empty;
            Buttons = buttons;
            OKText = "OK";
            CancelText = "CancelText";
            YesText = "Yes";
            NoText = "No";
            Result = OnSubmit ?? (_ => { });
        }
    }
}