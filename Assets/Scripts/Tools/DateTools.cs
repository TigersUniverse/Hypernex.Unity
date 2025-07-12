using System;
using System.Globalization;
using IHateUnityTime;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class DateTools : MonoBehaviour
    {
        // https://stackoverflow.com/a/250400/12968919
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
        
        public static bool Is24H { get; private set; }

        private static bool didError;

        private void Update()
        {
            if (didError)
            {
                Is24H = DateTimeFormatInfo.CurrentInfo.AMDesignator == "";
                return;
            }
            try
            {
                Is24H = UnityTimeHater.Is24HourClock();
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
                didError = true;
            }
        }
    }
}
