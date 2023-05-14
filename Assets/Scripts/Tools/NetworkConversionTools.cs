using Hypernex.Networking.Messages.Data;
using UnityEngine;

namespace Hypernex.Tools
{
    public static class NetworkConversionTools
    {
        public static float2 Vector2Tofloat2(Vector2 vector2) => new float2(vector2.x, vector2.y);
        public static float3 Vector3Tofloat3(Vector3 vector3) => new float3(vector3.x, vector3.y, vector3.z);
        public static float4 Vector4Tofloat4(Vector4 vector4) => new float4(vector4.x, vector4.y, vector4.z, vector4.w);
        public static float4 QuaternionTofloat4(Quaternion quaternion) =>
            new float4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        public static Vector2 float2ToVector2(float2 float2) => new Vector2(float2.x, float2.y);
        public static Vector3 float3ToVector3(float3 float3) => new Vector3(float3.x, float3.y, float3.z);
        public static Vector4 float4ToVector4(float4 float4) => new Vector4(float4.x, float4.y, float4.z, float4.w);
        public static Quaternion float4ToQuaternion(float4 float4) =>
            new Quaternion(float4.x, float4.y, float4.z, float4.w);
    }
}