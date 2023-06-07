using System;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class UtcTime
    {
        private DateTime d = DateTime.UtcNow;

        public string GetDayOfWeek() => d.DayOfWeek.ToString();
        public string GetMonthName() => d.ToString("MM");
        
        public int GetMilliseconds() => d.Millisecond;
        public int GetSeconds() => d.Second;
        public int GetMinutes() => d.Minute;
        public int GetHours() => d.Hour;
        public int GetMonth() => d.Month;
    }
}