using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class RaycastHit
    {
        internal UnityEngine.RaycastHit r;
        private bool read = true;

        public RaycastHit() => r = new UnityEngine.RaycastHit();
        internal RaycastHit(UnityEngine.RaycastHit r, bool read)
        {
            this.r = r;
            this.read = read;
        }

        public Collider collider => new Collider(r.collider, read);
        public float2 textureCoord => NetworkConversionTools.Vector2Tofloat2(r.textureCoord);
        public float2 textureCoord2 => NetworkConversionTools.Vector2Tofloat2(r.textureCoord2);
        public Item item => new Item(r.transform, read);
        public float2 lightmapCoord => NetworkConversionTools.Vector2Tofloat2(r.lightmapCoord);

        // These properties do not follow the read rules, they are safe to write to from anywhere
        public float3 point
        {
            get => NetworkConversionTools.Vector3Tofloat3(r.point);
            set => r.point = NetworkConversionTools.float3ToVector3(value);
        }
        public float3 normal
        {
            get => NetworkConversionTools.Vector3Tofloat3(r.normal);
            set => r.normal = NetworkConversionTools.float3ToVector3(value);
        }
        public float3 barycentricCoordinate
        {
            get => NetworkConversionTools.Vector3Tofloat3(r.barycentricCoordinate);
            set => r.barycentricCoordinate = NetworkConversionTools.float3ToVector3(value);
        }
        public float distance
        {
            get => r.distance;
            set => r.distance = value;
        }
    }
}