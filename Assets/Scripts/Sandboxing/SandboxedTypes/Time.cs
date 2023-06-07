using System;
using System.Globalization;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Time
    {
        private DateTime d = DateTime.Now;

        public bool Is24HourClock() => DateTimeFormatInfo.CurrentInfo.ShortTimePattern.Contains("H");

        public string GetDayOfWeek() => d.DayOfWeek.ToString();
        public string GetMonthName() => d.ToString("MM");
        public string GetAMPM() => d.ToString("tt", CultureInfo.InvariantCulture).ToUpper();
        
        public int GetMilliseconds() => d.Millisecond;
        public int GetSeconds() => d.Second;
        public int GetMinutes() => d.Minute;
        public int GetHours() => d.Hour;
        public int GetMonth() => d.Month;
    }
}