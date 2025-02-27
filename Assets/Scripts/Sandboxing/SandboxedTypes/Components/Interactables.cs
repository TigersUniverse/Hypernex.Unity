using System;
using Hypernex.Game;
using Hypernex.Player;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Interactables
    {
        private readonly bool read;
        private readonly NetworkSync networkSync;
        private readonly Respawnable respawnable;
        private readonly Grabbable grabbable;

        public Interactables(Item i)
        {
            read = i.IsReadOnly;
            networkSync = i.t.GetComponent<NetworkSync>();
            respawnable = i.t.GetComponent<Respawnable>();
            grabbable = i.t.GetComponent<Grabbable>();
        }
        
        public bool SyncEnabled
        {
            get => networkSync == null ? false : networkSync.enabled;
            set
            {
                if(read || networkSync == null) return;
                networkSync.enabled = value;
            }
        }
        
        public bool RespawnEnabled
        {
            get => respawnable == null ? false : respawnable.enabled;
            set
            {
                if(read || respawnable == null) return;
                respawnable.enabled = value;
            }
        }
        
        public bool GrabEnabled
        {
            get => grabbable == null ? false : grabbable.enabled;
            set
            {
                if(read || grabbable == null) return;
                grabbable.enabled = value;
            }
        }
        
        public void Respawn()
        {
            // Don't respawn if you don't have control
            if(read)
                return;
            if (networkSync != null && !networkSync.IsOwnedByLocalPlayer())
                return;
            if(respawnable != null)
                respawnable.Respawn();
        }

        public bool IsNetworkSyncOwned()
        {
            if (networkSync != null)
                return false;
            return networkSync.IsOwned();
        }
        
        public bool IsNetworkSyncOwnedLocally()
        {
            if (networkSync != null)
                return false;
            return networkSync.IsOwnedByLocalPlayer();
        }

        public string GetNetworkSyncOwner()
        {
            if (networkSync != null)
                return String.Empty;
            if (networkSync.IsOwnedByLocalPlayer())
                return APIPlayer.APIUser.Id;
            return networkSync.NetworkOwner;
        }

        public bool IsGrabbedLocally()
        {
            if (grabbable == null)
                return false;
            return grabbable.IsGrabbed();
        }
    }
}