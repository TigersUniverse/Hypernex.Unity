using System;
using System.Collections.Generic;
using Hypernex.Networking.Messages;
using HypernexSharp.APIObjects;
using Nexport;

namespace Hypernex.Databasing.Objects
{
    [Msg]
    public class AvatarConfiguration : IIndex
    {
        public const string TABLE = "AvatarConfigurations";
        [MsgKey(1)] public string Id { get; set; }
        [MsgKey(2)] public string GestureIdentifierOverride = String.Empty;
        [MsgKey(3)] public string SelectedWeight = String.Empty;
        [MsgKey(4)] public Dictionary<string, WeightedObjectUpdate[]> SavedWeights = new();
        
        public AvatarConfiguration(){}
        public AvatarConfiguration(AvatarMeta meta) => Id = meta.Id;
    }
}