using UnityEngine;

namespace Hypernex.Tools
{
    public class RotationOffsetDriver
    {
        public Transform target { get; }
        public Quaternion rootReference { get; }

        public RotationOffsetDriver(Transform target, Transform root)
        {
            this.target = target;
            rootReference = Quaternion.Inverse(root.rotation) * target.rotation;
        }

        public void Rotate(Quaternion rot) => target.rotation = rot * rootReference;
    }
}