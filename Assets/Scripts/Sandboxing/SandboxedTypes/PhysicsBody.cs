using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class PhysicsBody
    {
        private static Rigidbody GetBody(Item item)
        {
            Rigidbody a = item.t.GetComponent<Rigidbody>();
            if (a == null)
                return null;
            return a;
        }

        public static bool IsValid(Item item) => GetBody(item) != null;

        public static bool GetIsKinematic(Item item) => GetBody(item)?.isKinematic ?? default;
        public static void SetIsKinematic(Item item, bool val)
        {
            Rigidbody rb = GetBody(item);
            if (rb == null)
                return;
            rb.isKinematic = val;
        }

        public static bool GetUseGravity(Item item) => GetBody(item)?.useGravity ?? default;
        public static void SetUseGravity(Item item, bool val)
        {
            Rigidbody rb = GetBody(item);
            if (rb == null)
                return;
            rb.useGravity = val;
        }

        public static float3 GetVelocity(Item item) => NetworkConversionTools.Vector3Tofloat3(GetBody(item)?.velocity ?? default);
        public static void SetVelocity(Item item, float3 velocity)
        {
            Rigidbody rb = GetBody(item);
            if (rb == null)
                return;
            rb.velocity = NetworkConversionTools.float3ToVector3(velocity);
        }

        public static float3 GetAngularVelocity(Item item) => NetworkConversionTools.Vector3Tofloat3(GetBody(item)?.angularVelocity ?? default);
        public static void SetAngularVelocity(Item item, float3 velocity)
        {
            Rigidbody rb = GetBody(item);
            if (rb == null)
                return;
            rb.angularVelocity = NetworkConversionTools.float3ToVector3(velocity);
        }

        public static void ResetCenterOfMass(Item item) => GetBody(item)?.ResetCenterOfMass();
        public static float3 GetWorldCenterOfMass(Item item) => NetworkConversionTools.Vector3Tofloat3(GetBody(item)?.worldCenterOfMass ?? default);
        public static float3 GetCenterOfMass(Item item) => NetworkConversionTools.Vector3Tofloat3(GetBody(item)?.centerOfMass ?? default);
        public static void SetCenterOfMass(Item item, float3 velocity)
        {
            Rigidbody rb = GetBody(item);
            if (rb == null)
                return;
            rb.centerOfMass = NetworkConversionTools.float3ToVector3(velocity);
        }
    }
}