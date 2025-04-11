using UnityEngine;

namespace Hypernex.CCK.Unity.Descriptors
{
    public class NetworkSyncDescriptor : MonoBehaviour
    {
        [Tooltip("The Host of an instance will automatically network claim the GameObject, and not unclaim until they leave.")]
        public bool InstanceHostOnly;
        [Tooltip("Whether or not other Players can Claim a NetworkSync while it is already claimed.")]
        public bool CanSteal;
        [Tooltip("Always sync this GameObject over the network, even on Unclaim. " +
                 "Last person to Claim the GameObject will be in control of the GameObject.")]
        public bool AlwaysSync;
    }
}