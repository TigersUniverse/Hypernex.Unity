using System;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Collider
    {
        private const string WRITE_ERROR = "Cannot write when in readonly mode!";
        internal UnityEngine.Collider c;
        private bool read;

        public Collider() => c = new UnityEngine.Collider();
        internal Collider(UnityEngine.Collider c, bool read)
        {
            this.c = c;
            this.read = read;
        }

        public bool IsReadOnly => read;
        public Item item => new Item(c.transform, read);
        public bool isTrigger
        {
            get => c.isTrigger;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                c.isTrigger = value;
            }
        }
        public float contactOffset
        {
            get => c.contactOffset;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                c.contactOffset = value;
            }
        }
        public Bounds bounds => new Bounds(c.bounds, read);
        public bool hasModifiableContacts
        {
            get => c.hasModifiableContacts;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                c.hasModifiableContacts = value;
            }
        }
        public bool providesContacts
        {
            get => c.providesContacts;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                c.providesContacts = value;
            }
        }
        public int layerOverridePriority
        {
            get => c.layerOverridePriority;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                c.layerOverridePriority = value;
            }
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
            return new RaycastHit(g, read);
        }
        
        public static bool operator ==(Collider x, Collider y) => x?.Equals(y) ?? false;
        public static bool operator !=(Collider x, Collider y) => !(x == y);
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(Collider))
                return false;
            return c == ((Collider) obj).c;
        }
    }
}