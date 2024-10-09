using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class ContactPoint
    {
        private UnityEngine.ContactPoint c;
        private bool read;

        public ContactPoint() => c = new UnityEngine.ContactPoint();
        internal ContactPoint(UnityEngine.ContactPoint c, bool read)
        {
            this.c = c;
            this.read = read;
        }

        public float3 point => NetworkConversionTools.Vector3Tofloat3(c.point);
        public float3 normal => NetworkConversionTools.Vector3Tofloat3(c.normal);
        public float3 impulse => NetworkConversionTools.Vector3Tofloat3(c.impulse);
        public Collider thisCollider => new Collider(c.thisCollider, read);
        public Collider otherCollider => new Collider(c.otherCollider, read);
        public float separation => c.separation;
    }
}