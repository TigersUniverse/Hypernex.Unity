using System.Runtime.InteropServices;

namespace IHateUnityTime
{
    public static class UnityTimeHater
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private const string DLL = "IHateUnityTime.dll";
#else
        private const string DLL = "libIHateUnityTime.so";
#endif

        /// <summary>
        /// Checks to see if the current System Clock is in a 24 Hour Format
        /// </summary>
        /// <returns>True if 24 Hour format</returns>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Is24HourClock();
    }
}