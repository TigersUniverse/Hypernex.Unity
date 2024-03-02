using System;
using System.Collections.Generic;
using Hypernex.Networking.Messages.Data;
using UnityEngine;

namespace Hypernex.Tools
{
    public static class Extensions
    {
        internal static NetworkedObject GetNetworkTransform(this Transform transform, Transform root = null, bool global = true)
        {
            NetworkedObject networkedObject = new NetworkedObject
            {
                IgnoreObjectLocation = false,
                ObjectLocation = AnimationUtility.CalculateTransformPath(transform,
                    root == null ? AnimationUtility.GetRootOfChild(transform) : root)
            };
            networkedObject.Position = global
                ? NetworkConversionTools.Vector3Tofloat3(transform.position)
                : NetworkConversionTools.Vector3Tofloat3(transform.localPosition);
            networkedObject.Rotation = global
                ? NetworkConversionTools.QuaternionTofloat4(new Quaternion(transform.eulerAngles.x,
                    transform.eulerAngles.y, transform.eulerAngles.z, 0))
                : new float4(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z,
                    0);
            networkedObject.Size = NetworkConversionTools.Vector3Tofloat3(transform.localScale);
            return networkedObject;
        }

        internal static void Apply(this NetworkedObject networkedObject, SmoothTransform transform, bool global = true)
        {
            transform.Position = NetworkConversionTools.float3ToVector3(networkedObject.Position);
            transform.Rotation = Quaternion.Euler(new Vector3(networkedObject.Rotation.x,
                networkedObject.Rotation.y, networkedObject.Rotation.z));
            transform.Scale = NetworkConversionTools.float3ToVector3(networkedObject.Size);
        }

        internal static bool TryFind<T>(this List<T> list, Predicate<T> match, out T found)
        {
            foreach (T t in list)
            {
                if (match.Invoke(t))
                {
                    found = t;
                    return true;
                }
            }
            found = default;
            return false;
        }
    }
}