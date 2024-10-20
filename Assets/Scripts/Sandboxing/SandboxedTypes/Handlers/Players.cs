using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Game;
using Hypernex.Player;
using HypernexSharp.APIObjects;

namespace Hypernex.Sandboxing.SandboxedTypes.Handlers
{
    public class Players
    {
        private GameInstance gameInstance;
        private SandboxRestriction sandboxRestriction;
        private IPlayer avatarPlayer;

        public Players() => throw new Exception("Cannot instantiate Players!");
        internal Players(GameInstance gameInstance, SandboxRestriction sandboxRestriction, IPlayer avatarPlayer)
        {
            this.gameInstance = gameInstance;
            this.sandboxRestriction = sandboxRestriction;
            this.avatarPlayer = avatarPlayer;
        }

        public string[] ActiveUserIds => GameInstance.GetConnectedUsers(gameInstance).Select(x => x.Id).ToArray();

        private bool wasInInstance = true;
        private Dictionary<string, Player> playerCache = new();

        private void UpdatePlayerCache()
        {
            string[] userIds = ActiveUserIds;
            if (playerCache.Count == userIds.Length) return;
            if (playerCache.Count > userIds.Length)
            {
                // Someone left
                List<string> usersWhoLeft = new List<string>();
                foreach (KeyValuePair<string,Player> keyValuePair in playerCache)
                {
                    if(userIds.Contains(keyValuePair.Key)) continue;
                    usersWhoLeft.Add(keyValuePair.Key);
                }
                usersWhoLeft.ForEach(x => playerCache.Remove(x));
                return;
            }
            // Someone joined
            foreach (User activeUser in GameInstance.GetConnectedUsers(gameInstance))
            {
                if(playerCache.ContainsKey(activeUser.Id)) continue;
                if (activeUser.Id == APIPlayer.APIUser.Id)
                {
                    // Local Player
                    LocalPlayer lp = Hypernex.Game.LocalPlayer.Instance;
                    if(lp == null) continue;
                    playerCache.Add(activeUser.Id, new Player(gameInstance, sandboxRestriction, activeUser, lp));
                }
                else
                {
                    // Net Player
                    NetPlayer netPlayer = PlayerManagement.GetNetPlayer(gameInstance, activeUser.Id);
                    if(netPlayer == null) continue;
                    playerCache.Add(activeUser.Id, new Player(gameInstance, sandboxRestriction, activeUser, netPlayer));
                }
            }
        }

        public Player LocalPlayer
        {
            get
            {
                UpdatePlayerCache();
                switch (sandboxRestriction)
                {
                    case SandboxRestriction.Local:
                        if (!playerCache.ContainsKey(APIPlayer.APIUser.Id)) return null;
                        return playerCache[APIPlayer.APIUser.Id];
                    case SandboxRestriction.LocalAvatar:
                        if (avatarPlayer == null) return null;
                        if (!playerCache.ContainsKey(avatarPlayer.Id)) return null;
                        return playerCache[avatarPlayer.Id];
                }
                return null;
            }
        }

        public Player[] Children
        {
            get
            {
                UpdatePlayerCache();
                return playerCache.Values.ToArray();
            }
        }

        public Player GetPlayerFromUserId(string userId)
        {
            UpdatePlayerCache();
            foreach (KeyValuePair<string,Player> keyValuePair in playerCache)
            {
                if(keyValuePair.Key != userId) continue;
                return keyValuePair.Value;
            }
            return null;
        }
    }
}