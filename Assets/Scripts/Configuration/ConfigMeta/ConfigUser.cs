using System.Collections.Generic;
using Tomlet.Models;

namespace Hypernex.Configuration.ConfigMeta
{
    public class ConfigUser
    {
        public string Server;
        public string UserId;
        public string Username;
        public string TokenContent;

        public string CurrentAvatar;
        public string HomeWorld;
        public List<string> SavedAvatars = new ();
        public List<string> SavedWorlds = new ();

        public bool UseFacialTracking;
        public Dictionary<string, string> FacialTrackingSettings = new();

        public int EmojiType;

        public void Clone(ConfigUser c)
        {
            Server = c.Server;
            UserId = c.UserId;
            Username = c.Username;
            TokenContent = c.TokenContent;
            CurrentAvatar = c.CurrentAvatar;
            HomeWorld = c.HomeWorld;
            SavedAvatars = c.SavedAvatars;
            SavedWorlds = c.SavedWorlds;
            UseFacialTracking = c.UseFacialTracking;
            FacialTrackingSettings = c.FacialTrackingSettings;
            EmojiType = c.EmojiType;
        }
    }
}