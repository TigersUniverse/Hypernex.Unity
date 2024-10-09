using System;
using System.Collections.Generic;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Collision
    {
        private UnityEngine.Collision c;
        private bool read;

        public Collision()
        {
            throw new Exception("Cannot instantiate Collision!");
        }
        
        internal Collision(UnityEngine.Collision c, bool read)
        {
            this.c = c;
            this.read = read;
        }
        
        public float3 impulse => NetworkConversionTools.Vector3Tofloat3(c.impulse);
        public float3 relativeVelocity => NetworkConversionTools.Vector3Tofloat3(c.relativeVelocity);
        public Collider collider => new Collider(c.collider, read);
        public Item item => new Item(c.transform, read);
        public ContactPoint[] contacts
        {
            get
            {
                List<UnityEngine.ContactPoint> contactPoints = new List<UnityEngine.ContactPoint>();
                c.GetContacts(contactPoints);
                List<ContactPoint> sandboxContactPoints = new List<ContactPoint>();
                foreach (UnityEngine.ContactPoint contactPoint in contactPoints)
                    sandboxContactPoints.Add(new ContactPoint(contactPoint, read));
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