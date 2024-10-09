using System;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Bounds
    {
        private const string WRITE_ERROR = "Cannot write when in readonly mode!";
        
        private readonly bool read;
        internal UnityEngine.Bounds b;

        public Bounds() => b = new UnityEngine.Bounds();

        internal Bounds(UnityEngine.Bounds b, bool read)
        {
            this.read = read;
            this.b = b;
        }

        public float3 center
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.center);
            set
            {
                if (read) if (read) throw new Exception(WRITE_ERROR);
                b.center = NetworkConversionTools.float3ToVector3(value);
            }
        }
        public float3 size
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.size);
            set
            {
                if (read) if (read) throw new Exception(WRITE_ERROR);
                b.size = NetworkConversionTools.float3ToVector3(value);
            }
        }
        public float3 extents
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.extents);
            set
            {
                if (read) if (read) throw new Exception(WRITE_ERROR);
                b.extents = NetworkConversionTools.float3ToVector3(value);
            }
        }
        public float3 min
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.min);
            set
            {
                if (read) if (read) throw new Exception(WRITE_ERROR);
                b.min = NetworkConversionTools.float3ToVector3(value);
            }
        }
        public float3 max
        {
            get => NetworkConversionTools.Vector3Tofloat3(b.max);
            set
            {
                if (read) if (read) throw new Exception(WRITE_ERROR);
                b.max = NetworkConversionTools.float3ToVector3(value);
            }
        }

        public void SetMinMax(float3 _min, float3 _max)
        {
            if (read) if (read) throw new Exception(WRITE_ERROR);
            b.SetMinMax(NetworkConversionTools.float3ToVector3(_min), NetworkConversionTools.float3ToVector3(_max));
        }

        public void Encapsulate(float3 point)
        {
            if (read) if (read) throw new Exception(WRITE_ERROR);
            b.Encapsulate(NetworkConversionTools.float3ToVector3(point));
        }

        public void Encapsulate(Bounds bounds)
        {
            if (read) if (read) throw new Exception(WRITE_ERROR);
            Encapsulate(bounds.center - bounds.extents);
            Encapsulate(bounds.center + bounds.extents);
        }

        public void Expand(float3 amount)
        {
            if (read) if (read) throw new Exception(WRITE_ERROR);
            extents += amount * 0.5f;
        }

        public void Expand(float amount)
        {
            if (read) if (read) throw new Exception(WRITE_ERROR);
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