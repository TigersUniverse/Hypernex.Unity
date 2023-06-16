using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Collider
    {
        internal UnityEngine.Collider c;

        public Collider() => c = new UnityEngine.Collider();
        internal Collider(UnityEngine.Collider c) => this.c = c;

        public bool isTrigger
        {
            get => c.isTrigger;
            set => c.isTrigger = value;
        }
        public float contactOffset
        {
            get => c.contactOffset;
            set => c.contactOffset = value;
        }
        public Bounds bounds => new Bounds(c.bounds);
        public bool hasModifiableContacts
        {
            get => c.hasModifiableContacts;
            set => c.hasModifiableContacts = value;
        }
        public bool providesContacts
        {
            get => c.providesContacts;
            set => c.providesContacts = value;
        }
        public int layerOverridePriority
        {
            get => c.layerOverridePriority;
            set => c.layerOverridePriority = value;
        }

        public float3 ClosestPoint(float3 position) =>
            NetworkConversionTools.Vector3Tofloat3(c.ClosestPoint(NetworkConversionTools.float3ToVector3(position)));

        public float3 ClosestPointOnBounds(float3 position) =>
            NetworkConversionTools.Vector3Tofloat3(
                c.ClosestPointOnBounds(NetworkConversionTools.float3ToVector3(position)));

        public RaycastHit Raycast(Ray ray, float maxDistance)
        {
            UnityEngine.RaycastHit g;
            bool hit = c.Raycast(ray.r, out g, maxDistance);
            if (!hit)
                return null;
            return new RaycastHit(g);
        }
    }
}