using System;
using System.Collections.Generic;
using System.Text;
using Discord.GameSDK;
using Discord.GameSDK.Activities;
using Hypernex.Player;
using HypernexSharp.APIObjects;

namespace Hypernex.Tools
{
#if UNITY_ANDROID || UNITY_EDITOR
    internal static class DiscordTools
    {
        public static void StartDiscord()
        {
        }

        internal static void FocusInstance(WorldMeta worldMeta, string id, User host)
        {
        }

        internal static void UnfocusInstance(string id)
        {
        }

        internal static void RunCallbacks()
        {
        }

        internal static void Stop()
        {
        }
    }
#else
    internal static class DiscordTools
    {
        private const long DiscordApplicationId = 1101618498062516254;
        private static readonly Discord.GameSDK.Discord discord = new (DiscordApplicationId, CreateFlags.NoRequireDiscord);
        private static readonly long startTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

        private static bool ignoreUserRefresh;
        private static readonly Dictionary<string, long> InstanceDateTimes = new();

        private static string GetSpacedStatus(Status status)
        {
            StringBuilder statusSpaced = new StringBuilder();
            for (int i = 0; i < status.ToString().Length; i++)
            {
                char c = status.ToString()[i];
                if (i != 0 && char.IsUpper(c))
                {
                    statusSpaced.Append(" ");
                    statusSpaced.Append(c);
                }
                else
                    statusSpaced.Append(c);
            }
            return statusSpaced.ToString();
        }

        private static void DefaultActivity(User user)
        {
            try
            {
                string status = user.Bio.Status.ToString();
                string statusSpaced = GetSpacedStatus(user.Bio.Status);
                discord.GetActivityManager().UpdateActivity(new Activity
                {
                    Name = "Hypernex",
                    Details = $"Playing as {user.Username}",
                    Timestamps = new ActivityTimestamps {Start = startTime},
                    Assets = new ActivityAssets
                    {
                        LargeImage = "logo",
                        SmallImage = status.ToLower(),
                        SmallText = string.IsNullOrEmpty(APIPlayer.APIUser.Bio.StatusText)
                            ? statusSpaced
                            : APIPlayer.APIUser.Bio.StatusText
                    }
                }, result => { });
            } catch(Exception){}
        }

        public static void StartDiscord()
        {
            try
            {
                if (Discord.GameSDK.Discord.IsInitialized)
                    return;
                discord.Init();
                discord.GetActivityManager().UpdateActivity(new Activity
                {
                    Name = "Hypernex",
                    Details = "Logging In",
                    Timestamps = new ActivityTimestamps {Start = startTime},
                    Assets = new ActivityAssets {LargeImage = "logo"}
                }, result => {});
                APIPlayer.OnUserRefresh += user =>
                {
                    if (ignoreUserRefresh)
                        return;
                    DefaultActivity(user);
                };
                APIPlayer.OnLogout += () =>
                {
                    ignoreUserRefresh = false;
                    InstanceDateTimes.Clear();
                    discord.GetActivityManager().UpdateActivity(new Activity
                    {
                        Name = "Hypernex",
                        Details = "Logging In",
                        Timestamps = new ActivityTimestamps {Start = startTime},
                        Assets = new ActivityAssets {LargeImage = "logo"}
                    }, result => { });
                };
            } catch(Exception){}
        }

        internal static void FocusInstance(WorldMeta worldMeta, string id, User host)
        {
            try
            {
                if (!Discord.GameSDK.Discord.IsInitialized)
                    return;
                ignoreUserRefresh = true;
                long time;
                if (InstanceDateTimes.ContainsKey(id))
                    time = InstanceDateTimes[id];
                else
                {
                    time = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                    InstanceDateTimes.Add(id, time);
                }
                string status = APIPlayer.APIUser.Bio.Status.ToString();
                string statusSpaced = GetSpacedStatus(APIPlayer.APIUser.Bio.Status);
                discord.GetActivityManager().UpdateActivity(new Activity
                {
                    Name = "Hypernex",
                    Details = $"Playing as {APIPlayer.APIUser.Username}",
                    Timestamps = new ActivityTimestamps {Start = time},
                    State = "Visiting " + worldMeta.Name,
                    Assets = new ActivityAssets
                    {
                        LargeImage = string.IsNullOrEmpty(worldMeta.ThumbnailURL) ? "logo" : worldMeta.ThumbnailURL,
                        LargeText = $"Hosted By {host.Username}",
                        SmallImage = status.ToLower(),
                        SmallText = string.IsNullOrEmpty(APIPlayer.APIUser.Bio.StatusText)
                            ? statusSpaced
                            : APIPlayer.APIUser.Bio.StatusText
                    }
                }, result => { });
            } catch(Exception){}
        }

        internal static void UnfocusInstance(string id)
        {
            ignoreUserRefresh = false;
            DefaultActivity(APIPlayer.APIUser);
            if (InstanceDateTimes.ContainsKey(id))
                InstanceDateTimes.Remove(id);
        }

        internal static void RunCallbacks()
        {
            try
            {
                if(Discord.GameSDK.Discord.IsInitialized)
                    discord.RunCallbacks();
            } catch(Exception){}
        }

        internal static void Stop()
        {
            try
            {
                if(Discord.GameSDK.Discord.IsInitialized)
                    discord.Dispose();
            } catch(Exception){}
        }
    }
#endif
}