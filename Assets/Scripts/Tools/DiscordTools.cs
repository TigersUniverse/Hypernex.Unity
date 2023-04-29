using System;
using System.Collections.Generic;
using System.Text;
using Discord.GameSDK;
using Discord.GameSDK.Activities;
using Hypernex.Player;
using HypernexSharp.APIObjects;

namespace Hypernex.Tools
{
    internal static class DiscordTools
    {
        private const long DiscordApplicationId = 1101618498062516254;
        private static readonly Discord.GameSDK.Discord discord = new (DiscordApplicationId, CreateFlags.Default);
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
                    SmallText = statusSpaced
                }
            }, result => { });
        }

        public static void StartDiscord()
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
        }

        internal static void FocusInstance(WorldMeta worldMeta, SafeInstance safeInstance, User host)
        {
            ignoreUserRefresh = true;
            long time;
            if (InstanceDateTimes.ContainsKey(safeInstance.InstanceId))
                time = InstanceDateTimes[safeInstance.InstanceId];
            else
            {
                time = new DateTimeOffset(new()).ToUnixTimeMilliseconds();
                InstanceDateTimes.Add(safeInstance.InstanceId, time);
            }
            string status = APIPlayer.APIUser.Bio.Status.ToString();
            string statusSpaced = GetSpacedStatus(APIPlayer.APIUser.Bio.Status);
            discord.GetActivityManager().UpdateActivity(new Activity
            {
                Name = "Hypernex",
                Details = $"Playing as {APIPlayer.APIUser.Username}",
                Timestamps = new ActivityTimestamps {Start = time},
                State = worldMeta.Name,
                Assets = new ActivityAssets
                {
                    LargeImage = string.IsNullOrEmpty(worldMeta.ThumbnailURL) ? "logo" : worldMeta.ThumbnailURL,
                    LargeText = $"Hosted By {host.Username}",
                    SmallImage = status.ToLower(),
                    SmallText = statusSpaced
                }
            }, result => { });
        }

        internal static void UnfocusInstance(SafeInstance instance = null)
        {
            ignoreUserRefresh = false;
            DefaultActivity(APIPlayer.APIUser);
            if (instance != null)
                InstanceDateTimes.Remove(instance.InstanceId);
        }

        internal static void RunCallbacks()
        {
            if(Discord.GameSDK.Discord.IsInitialized)
                discord.RunCallbacks();
        }

        internal static void Stop()
        {
            if(Discord.GameSDK.Discord.IsInitialized)
                discord.Dispose();
        }
    }
}