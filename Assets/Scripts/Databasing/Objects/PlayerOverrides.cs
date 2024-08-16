using Nexport;

namespace Hypernex.Databasing.Objects
{
    [Msg]
    public class PlayerOverrides : IIndex
    {
        public const string TABLE = "PlayerOverrides";
        [MsgKey(1)] public string Id { get; set; }
        [MsgKey(2)] public float Volume = 1f;
        
        public PlayerOverrides(){}
        public PlayerOverrides(string userId) => Id = userId;
    }
}