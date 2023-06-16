using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class RaycastHit
    {
        internal UnityEngine.RaycastHit r;

        public RaycastHit() => r = new UnityEngine.RaycastHit();
        internal RaycastHit(UnityEngine.RaycastHit r) => this.r = r;

        public Collider collider => new Collider(r.collider);
        public float2 textureCoord => NetworkConversionTools.Vector2Tofloat2(r.textureCoord);
        public float2 textureCoord2 => NetworkConversionTools.Vector2Tofloat2(r.textureCoord2);
        public ReadonlyItem item => new ReadonlyItem(r.transform);
        public float2 lightmapCoord => NetworkConversionTools.Vector2Tofloat2(r.lightmapCoord);

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