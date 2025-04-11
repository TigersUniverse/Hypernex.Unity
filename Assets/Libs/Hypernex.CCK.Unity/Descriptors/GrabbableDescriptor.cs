using UnityEngine;

namespace Hypernex.CCK.Unity.Descriptors
{
    public class GrabbableDescriptor : MonoBehaviour
    {
        [Tooltip("Apply Velocity to the GameObject's RigidBody (if present) after release. This allows objects to be thrown.")]
        public bool ApplyVelocity = true;
        [Tooltip("The amount of Velocity to be applied to the GameObject's RigidBidy.")]
        public float VelocityAmount = 10f;
        [Tooltip("The threshold for when to apply velocity. For example, when standing still, we don't want to apply any velocity. " +
                 "This should change based on your game's default WalkSpeed.")]
        public float VelocityThreshold = 0.05f;
        [Tooltip("Whether or not a player can grab this Grabbable with their laser.")]
        public bool GrabByLaser = true;
        [Tooltip("From how far away a Player can grab the Grabbable with their laser.")]
        public float LaserGrabDistance = 5f;
        [Tooltip("Whether or not a player can grab this Grabbable by distance. Imagine picking up a mug.")]
        public bool GrabByDistance = true;
        [Tooltip("From how far away a Player can grab the Grabbable without their laser.")]
        public float GrabDistance = 3f;
    }
}