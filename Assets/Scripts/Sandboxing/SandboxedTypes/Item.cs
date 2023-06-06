using System;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Item
    {
        internal Transform t;

        public Item()
        {
            throw new Exception("Item cannot be created by a Script!");
        }

        internal Item(Transform t) => this.t = t;
        
        public float3 Position
        {
            get => NetworkConversionTools.Vector3Tofloat3(t.position);
            set => t.position = NetworkConversionTools.float3ToVector3(value);
        }

        public float4 Rotation
        {
            get => NetworkConversionTools.QuaternionTofloat4(t.rotation);
            set => t.rotation = NetworkConversionTools.float4ToQuaternion(value);
        }

        public float3 LocalPosition
        {
            get => NetworkConversionTools.Vector3Tofloat3(t.localPosition);
            set => t.localPosition = NetworkConversionTools.float3ToVector3(value);
        }
        
        public float4 LocalRotation
        {
            get => NetworkConversionTools.QuaternionTofloat4(t.localRotation);
            set => t.localRotation = NetworkConversionTools.float4ToQuaternion(value);
        }

        public float3 LocalSize
        {
            get => NetworkConversionTools.Vector3Tofloat3(t.localScale);
            set => t.localScale = NetworkConversionTools.float3ToVector3(value);
        }
    }
}