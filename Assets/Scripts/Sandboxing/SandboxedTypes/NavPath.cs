using System;
using System.Linq;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine.AI;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class NavPath
    {
        internal NavMeshPath r;

        public NavPath() => r = new NavMeshPath();
        internal NavPath(NavMeshPath r)
        {
            this.r = r;
        }

        public float3[] Corners => r.corners.Select(x => NetworkConversionTools.Vector3Tofloat3(x)).ToArray();
        public NavMeshPathStatus Status => r.status;

        public void ClearCorners() => r.ClearCorners();
    }
}