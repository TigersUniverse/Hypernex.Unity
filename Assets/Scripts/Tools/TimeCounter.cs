using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(TMP_Text))]
    public class TimeCounter : MonoBehaviour
    {
        public static string GetTime(DateTime dateTime)
        {
            if (DateTools.Is24H)
                return dateTime.ToString("HH:mm:ss");
            return dateTime.ToString("h:mm:ss tt");
        }

        public static string GetDate(DateTime dateTime) =>
            dateTime.ToString(DateTimeFormatInfo.CurrentInfo.LongDatePattern.CastIf24Hour(DateTools.Is24H));
        
        private TMP_Text text;

        private void OnEnable() => text = GetComponent<TMP_Text>();

        private void Update() => text.text = GetTime(DateTime.Now);
    }
}