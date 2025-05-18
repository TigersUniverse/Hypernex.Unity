using System;
using Hypernex.Game;
using Hypernex.Player;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Interactables
    {
        private readonly bool read;
        private NetworkSync networkSync;
        private Respawnable respawnable;
        private Grabbable grabbable;
        private readonly Item item;

        private void FindComponents()
        {
            networkSync = item.t.GetComponent<NetworkSync>();
            respawnable = item.t.GetComponent<Respawnable>();
            grabbable = item.t.GetComponent<Grabbable>();
        }

        public Interactables(Item i)
        {
            item = i;
            read = i.IsReadOnly;
            FindComponents();
        }
        
        public bool SyncEnabled
        {
            get
            {
                if(networkSync == null) FindComponents();
                return networkSync == null ? false : networkSync.enabled;
            }
            set
            {
                if(read || networkSync == null)
                {
                    if(networkSync == null) FindComponents();
                    return;
                }
                networkSync.enabled = value;
            }
        }
        
        public bool RespawnEnabled
        {
            get
            {
                if(respawnable == null) FindComponents();
                return respawnable == null ? false : respawnable.enabled;
            }
            set
            {
                if(read || respawnable == null)
                {
                    if(respawnable == null) FindComponents();
                    return;
                }
                respawnable.enabled = value;
            }
        }
        
        public bool GrabEnabled
        {
            get
            {
                if(grabbable == null) FindComponents();
                return grabbable == null ? false : grabbable.enabled;
            }
            set
            {
                if(read || grabbable == null)
                {
                    if(grabbable == null) FindComponents();
                    return;
                }
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
            if(networkSync == null) FindComponents();
            if(respawnable != null)
                respawnable.Respawn();
        }

        public bool IsNetworkSyncOwned()
        {
            if (networkSync != null)
            {
                FindComponents();
                if(networkSync == null) return false;
            }
            return networkSync.IsOwned();
        }
        
        public bool IsNetworkSyncOwnedLocally()
        {
            if (networkSync != null)
            {
                FindComponents();
                if(networkSync == null) return false;
            }
            return networkSync.IsOwnedByLocalPlayer();
        }

        public string GetNetworkSyncOwner()
        {
            if (networkSync != null)
            {
                FindComponents();
                if(networkSync == null) return String.Empty;
            }
            if (networkSync.IsOwnedByLocalPlayer())
                return APIPlayer.APIUser.Id;
            return networkSync.NetworkOwner;
        }

        public bool IsGrabbedLocally()
        {
            if (grabbable == null)
            {
                FindComponents();
                if(grabbable == null) return false;
            }
            return grabbable.IsGrabbed();
        }
    }
}