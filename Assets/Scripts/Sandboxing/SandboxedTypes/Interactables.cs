using System;
using Hypernex.Game;
using Hypernex.Player;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Interactables
    {
        public static void Respawn(Item item)
        {
            // Don't respawn if you don't have control
            NetworkSync networkSync = item.t.gameObject.GetComponent<NetworkSync>();
            if (networkSync != null && !networkSync.IsOwnedByLocalPlayer())
                return;
            Respawnable respawnable = item.t.gameObject.GetComponent<Respawnable>();
            if(respawnable != null)
                respawnable.Respawn();
        }

        public static bool IsNetworkSyncOwned(Item item)
        {
            NetworkSync networkSync = item.t.gameObject.GetComponent<NetworkSync>();
            if (networkSync != null)
                return false;
            return networkSync.IsOwned();
        }
        
        public static bool IsNetworkSyncOwnedLocally(Item item)
        {
            NetworkSync networkSync = item.t.gameObject.GetComponent<NetworkSync>();
            if (networkSync != null)
                return false;
            return networkSync.IsOwnedByLocalPlayer();
        }

        public static string GetNetworkSyncOwner(Item item)
        {
            NetworkSync networkSync = item.t.gameObject.GetComponent<NetworkSync>();
            if (networkSync != null)
                return String.Empty;
            if (networkSync.IsOwnedByLocalPlayer())
                return APIPlayer.APIUser.Id;
            return networkSync.NetworkOwner;
        }

        public static bool IsGrabbedLocally(Item item)
        {
            Grabbable grabbable = item.t.GetComponent<Grabbable>();
            if (grabbable == null)
                return false;
            return grabbable.IsGrabbed();
        }
    }
}