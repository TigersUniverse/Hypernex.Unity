using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HypernexSharp.APIObjects;
using SimpleJSON;

namespace Hypernex.Tools
{
    public class GeoTools
    {
        private const string GEOIP = "https://api.seeip.org/geoip";
        private static float Latitude;
        private static float Longitude;

        private static HttpClient client = new();
        private static bool set;

        internal static void Init()
        {
            Task.Factory.StartNew(async () =>
            {
                string r = await client.GetStringAsync(GEOIP);
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    JSONNode n = JSON.Parse(r);
                    Latitude = n["latitude"].AsFloat;
                    Longitude = n["longitude"].AsFloat;
                    set = true;
                }));
            });
        }

        public static void SortGameServers(ref List<GameServer> gameServers)
        {
            if(gameServers.Count < 1 || !set || Latitude == 0 && Longitude == 0)
                return;
            gameServers.Sort((a, b) =>
            {
                bool d1z = a.Region.Latitude == 0 && a.Region.Longitude == 0;
                bool d2z = b.Region.Latitude == 0 && b.Region.Longitude == 0;
                if (d1z && !d2z)
                    return 1;
                if (!d1z && d2z)
                    return -1;
                if (d1z && d2z)
                    return 0;
                float d1 = a.Region.GetDistance(Latitude, Longitude);
                float d2 = b.Region.GetDistance(Latitude, Longitude);
                return d1.CompareTo(d2);
            });
        }
    }
}