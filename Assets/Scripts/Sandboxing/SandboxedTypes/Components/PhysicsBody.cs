using System;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class PhysicsBody
    {
        
        private readonly bool read;
        private Rigidbody rigidbody;
        
        public PhysicsBody(Item i)
        {
            read = i.IsReadOnly;
            rigidbody = i.t.GetComponent<Rigidbody>();
            if (rigidbody == null) throw new Exception("No PhysicsBody found on Item at " + i.Path);
        }

        public bool GetIsKinematic() => rigidbody.isKinematic;
        public void SetIsKinematic(bool val)
        {
            if(read)
                return;
            rigidbody.isKinematic = val;
        }

        public bool GetUseGravity() => rigidbody.useGravity;
        public void SetUseGravity(bool val)
        {
            if (read)
                return;
            rigidbody.useGravity = val;
        }

        public float3 GetVelocity() => NetworkConversionTools.Vector3Tofloat3(rigidbody.velocity);
        public void SetVelocity(float3 velocity)
        {
            if (read)
                return;
            rigidbody.velocity = NetworkConversionTools.float3ToVector3(velocity);
        }

        public float3 GetAngularVelocity() => NetworkConversionTools.Vector3Tofloat3(rigidbody.angularVelocity);
        public void SetAngularVelocity(float3 velocity)
        {
            if (read)
                return;
            rigidbody.angularVelocity = NetworkConversionTools.float3ToVector3(velocity);
        }

        public void ResetCenterOfMass()
        {
            if(read)
                return;
            rigidbody.ResetCenterOfMass();
        }
        public float3 GetWorldCenterOfMass() => NetworkConversionTools.Vector3Tofloat3(rigidbody.worldCenterOfMass);
        public float3 GetCenterOfMass() => NetworkConversionTools.Vector3Tofloat3(rigidbody.centerOfMass);
        public void SetCenterOfMass(float3 velocity)
        {
            if (read)
                return;
            rigidbody.centerOfMass = NetworkConversionTools.float3ToVector3(velocity);
        }
    }
}