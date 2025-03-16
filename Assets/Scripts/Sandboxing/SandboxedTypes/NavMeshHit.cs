using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class NavMeshHit
    {
        internal UnityEngine.AI.NavMeshHit r;

        public NavMeshHit() => r = new UnityEngine.AI.NavMeshHit();
        internal NavMeshHit(UnityEngine.AI.NavMeshHit r)
        {
            this.r = r;
        }

        public float3 Position
        {
            get => NetworkConversionTools.Vector3Tofloat3(r.position);
            set => r.position = NetworkConversionTools.float3ToVector3(value);
        }
        public float3 Normal
        {
            get => NetworkConversionTools.Vector3Tofloat3(r.normal);
            set => r.normal = NetworkConversionTools.float3ToVector3(value);
        }
        public float Distance
        {
            get => r.distance;
            set => r.distance = value;
        }
        public int Mask
        {
            get => r.mask;
            set => r.mask = value;
        }
        public bool Hit
        {
            get => r.hit;
            set => r.hit = value;
        }
    }
}