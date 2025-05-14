using System;
using System.Globalization;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Abstraction
{
    public class WarnRender : UIRender, IRender<WarnStatus>
    {
        public TMP_Text Header;
        public TMP_Text Reasoning;
        public GameObject ConfirmButton;
        public Action Confirmed;
        
        public void Render(WarnStatus t)
        {
            if (Header != null)
                Header.text = "You have been warned!";
            if (Reasoning != null)
            {
                string warnedTime = DateTools.UnixTimeStampToDateTime(t.TimeWarned)
                    .ToString(CultureInfo.InvariantCulture);
                Reasoning.text = $"Date Warned: {warnedTime}\n" +
                                 $"Reason: {t.WarnReason}\n" +
                                 $"Description: {t.WarnDescription}";
            }
            ConfirmButton.SetActive(true);
        }

        public void OnConfirm()
        {
            Confirmed?.Invoke();
            ConfirmButton.SetActive(false);
            Confirmed = null;
        }
    }
}