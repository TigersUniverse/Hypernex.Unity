using System;
using System.Collections.Generic;
using Hypernex.Game;
using Hypernex.Sandboxing.SandboxedTypes.Handlers;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class InstanceContainer
    {
        public readonly Runtime Runtime;
        private readonly Bindings bindings;
        
        private readonly Dictionary<string, object> Handlers = new();
        private GameInstance gameInstance;
        private SandboxRestriction sandboxRestriction;
        private IPlayer avatarPlayer;

        public InstanceContainer() { throw new Exception("Cannot instantiate InstanceContainer! "); }
        internal InstanceContainer(GameInstance gameInstance, SandboxRestriction sandboxRestriction, IPlayer avatarPlayer)
        {
            this.gameInstance = gameInstance;
            this.sandboxRestriction = sandboxRestriction;
            this.avatarPlayer = avatarPlayer;
            Runtime = new Runtime();
            bindings = new Bindings(sandboxRestriction == SandboxRestriction.Local ? LocalPlayer.Instance : avatarPlayer);
            if(gameInstance != null)
            {
                switch (sandboxRestriction)
                {
                    case SandboxRestriction.Local:
                        Handlers.Add("Network", new ClientNetworkEvent(gameInstance));
                        Handlers.Add("Events", gameInstance.LocalScriptEvents);
                        break;
                    case SandboxRestriction.LocalAvatar:
                        Handlers.Add("Events", gameInstance.AvatarScriptEvents);
                        break;
                }
                Handlers.Add("World", new World(gameInstance, sandboxRestriction));
            }
            Handlers.Add("Physics", new Physics(gameInstance, sandboxRestriction));
            Handlers.Add("Players", new Players(gameInstance, sandboxRestriction, avatarPlayer));
            Handlers.Add("Runtime", Runtime);
            Handlers.Add("Bindings", bindings);
        }

        public object GetHandler(string handler)
        {
            if (Handlers.TryGetValue(handler, out object h)) return h;
            throw new Exception("No Handler found for " + handler);
        }

        internal void Dispose()
        {
            // Call this first, because of OnDispose event
            Runtime.Dispose();
            bindings.Dispose();
        }
    }
}