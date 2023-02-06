using System.Collections.Generic;
using Tomlet.Attributes;

public class Config
{
    [TomlProperty("SavedServers")]
    public List<string> SavedServers { get; set; } = new();

    [TomlProperty("SavedAccounts")]
    public List<ConfigUser> SavedAccounts { get; set; } = new();
}