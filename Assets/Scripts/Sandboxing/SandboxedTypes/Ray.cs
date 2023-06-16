using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Ray
    {
        internal UnityEngine.Ray r;

        public Ray() => r = new UnityEngine.Ray();
        internal Ray(UnityEngine.Ray r) => this.r = r;
        
        public float3 origin
        {
            get => NetworkConversionTools.Vector3Tofloat3(r.origin);
            set => r.origin = NetworkConversionTools.float3ToVector3(value);
        }
        public float3 direction
        {
            get => NetworkConversionTools.Vector3Tofloat3(r.direction);
            set => r.direction = NetworkConversionTools.float3ToVector3(value);
        }

        public float3 GetPoint(float distance) => NetworkConversionTools.Vector3Tofloat3(r.GetPoint(distance));
    }
}