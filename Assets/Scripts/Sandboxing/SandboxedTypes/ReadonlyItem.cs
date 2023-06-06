using System;
using Hypernex.Networking.Messages.Data;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class ReadonlyItem
    {
        private Item item;

        public ReadonlyItem()
        {
            throw new Exception("Item cannot be created by a Script!");
        }

        internal ReadonlyItem(Transform transform) => item = new Item(transform);

        public float3 Position => item.Position;
        public float4 Rotation => item.Rotation;

        public float3 LocalPosition => item.LocalPosition;
        public float4 LocalRotation => item.LocalRotation;
        public float3 LocalSize => item.LocalSize;
    }
}