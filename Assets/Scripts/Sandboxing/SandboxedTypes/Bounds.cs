using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Bounds
    {
        internal UnityEngine.Bounds b;

        public Bounds() => b = new UnityEngine.Bounds();
        internal Bounds(UnityEngine.Bounds b) => this.b = b;

        public float3 center
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.center);
            set => b.center = NetworkConversionTools.float3ToVector3(value);
        }
        public float3 size
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.size);
            set => b.size = NetworkConversionTools.float3ToVector3(value);
        }
        public float3 extents
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.extents);
            set => b.extents = NetworkConversionTools.float3ToVector3(value);
        }
        public float3 min
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.min);
            set => b.min = NetworkConversionTools.float3ToVector3(value);
        }
        public float3 max
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.max);
            set => b.max = NetworkConversionTools.float3ToVector3(value);
        }

        public void SetMinMax(float3 _min, float3 _max) => b.SetMinMax(NetworkConversionTools.float3ToVector3(_min),
            NetworkConversionTools.float3ToVector3(_max));

        public void Encapsulate(float3 point) => b.Encapsulate(NetworkConversionTools.float3ToVector3(point));

        public void Encapsulate(Bounds bounds)
        {
            Encapsulate(bounds.center - bounds.extents);
            Encapsulate(bounds.center + bounds.extents);
        }

        public void Expand(float3 amount) => extents += amount * 0.5f;

        public void Expand(float amount)
        {
            float newAmount = amount * 0.5f;
            extents += new float3(newAmount, newAmount, newAmount);
        }

        public bool Intersects(Bounds bounds) => min.x <= bounds.max.x && max.x >= bounds.min.x &&
                                                 min.y <= bounds.max.y && max.y >= bounds.min.y &&
                                                 min.z <= bounds.max.z && max.z >= bounds.min.z;

        public float IntersectRay(Ray ray)
        {
            float distance;
            b.IntersectRay(ray.r, out distance);
            return distance;
        }

        public bool Contains(float3 point) => b.Contains(NetworkConversionTools.float3ToVector3(point));

        public float SqrDistance(float3 point) => b.SqrDistance(NetworkConversionTools.float3ToVector3(point));

        public float3 ClosestPoint(float3 point) =>
            NetworkConversionTools.Vector3Tofloat3(b.ClosestPoint(NetworkConversionTools.float3ToVector3(point)));
        
        public static bool operator ==(Bounds lhs, Bounds rhs) => lhs!.center == rhs!.center && lhs.extents == rhs.extents;
        public static bool operator !=(Bounds lhs, Bounds rhs) => !(lhs == rhs);
    }
}