using System.Collections.Generic;
using Hypernex.Configuration.ConfigMeta;
using Tomlet.Attributes;

namespace Hypernex.Configuration
{
    public class Config
    {
        [TomlProperty("SavedServers")]
        public List<string> SavedServers { get; set; } = new();

        [TomlProperty("SavedAccounts")]
        public List<ConfigUser> SavedAccounts { get; set; } = new();
    }
}