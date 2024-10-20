using Hypernex.Game.Avatar;
using UnityEngine;

namespace Hypernex.Game
{
    public interface IPlayer
    {
        public bool IsLocal { get; }
        public string Id { get; }
        public AvatarCreator AvatarCreator { get; }
        public Transform transform { get; }
        public bool IsLoadingAvatar { get; }
        public float AvatarDownloadPercentage { get; }
    }
}