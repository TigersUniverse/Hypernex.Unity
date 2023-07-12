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
            /*if (Is24H)
                return DateTime.Now.ToString("HH:mm:ss");
            return DateTime.Now.ToString("h:mm:ss tt");*/
            return dateTime.ToString(DateTimeFormatInfo.CurrentInfo.ShortTimePattern);
        }

        public static string GetDate(DateTime dateTime) =>
            dateTime.ToString(DateTimeFormatInfo.CurrentInfo.LongDatePattern);
        
        public static bool Is24H => DateTimeFormatInfo.CurrentInfo.ShortTimePattern.Contains("H");
        
        private TMP_Text text;

        private void OnEnable() => text = GetComponent<TMP_Text>();

        private void Update() => text.text = GetTime(DateTime.Now);
    }
}