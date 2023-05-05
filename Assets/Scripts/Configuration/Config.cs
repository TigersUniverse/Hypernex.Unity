using System.Collections.Generic;
using Hypernex.Configuration.ConfigMeta;
using Tomlet.Attributes;

namespace Hypernex.Configuration
{
    public class Config
    {
        [TomlProperty("CurrentAvatarId")]
        public string CurrentAvatarId { get; set; }

        [TomlProperty("FavoriteAvatarIds")] 
        public List<string> FavoriteAvatarIds { get; set; } = new();
        
        [TomlProperty("HomeWorldId")]
        public string HomeWorldId { get; set; }

        [TomlProperty("FavoriteWorldIds")]
        public List<string> FavoriteWorldIds { get; set; } = new ();
        
        [TomlProperty("SelectedMicrophone")]
        public string SelectedMicrophone { get; set; }

        [TomlProperty("DownloadThreads")]
        public int DownloadThreads { get; set; } = 50;

        [TomlProperty("MaxMemoryStorageCache")]
        public int MaxMemoryStorageCache { get; set; } = 5120;
        
        [TomlProperty("SavedServers")]
        public List<string> SavedServers { get; set; } = new();

        [TomlProperty("SavedAccounts")]
        public List<ConfigUser> SavedAccounts { get; set; } = new();
    }
}