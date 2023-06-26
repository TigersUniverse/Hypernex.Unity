using System.Collections.Generic;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Collision
    {
        private UnityEngine.Collision c;
        
        public Collision(){}
        internal Collision(UnityEngine.Collision c) => this.c = c;
        
        public float3 impulse => NetworkConversionTools.Vector3Tofloat3(c.impulse);
        public float3 relativeVelocity => NetworkConversionTools.Vector3Tofloat3(c.relativeVelocity);
        public Collider collider => new Collider(c.collider);
        public ReadonlyItem item => new ReadonlyItem(c.transform);
        public ContactPoint[] contacts
        {
            get
            {
                List<UnityEngine.ContactPoint> contactPoints = new List<UnityEngine.ContactPoint>();
                c.GetContacts(contactPoints);
                List<ContactPoint> sandboxContactPoints = new List<ContactPoint>();
                foreach (UnityEngine.ContactPoint contactPoint in contactPoints)
                    sandboxContactPoints.Add(new ContactPoint(contactPoint));
                return sandboxContactPoints.ToArray();
            }
        }
        
        public static bool operator ==(Collision x, Collision y) => x?.Equals(y) ?? false;
        public static bool operator !=(Collision x, Collision y) => !(x == y);
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(Collision))
                return false;
            return c == ((Collision) obj).c;
        }
    }
}