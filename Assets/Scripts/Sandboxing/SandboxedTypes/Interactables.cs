using Hypernex.Game;

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
    }
}