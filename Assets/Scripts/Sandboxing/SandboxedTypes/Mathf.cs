using System;
using Hypernex.Networking.Messages.Data;
using Unity.Mathematics;
using UnityEngine;
using float2 = Hypernex.Networking.Messages.Data.float2;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class ClientMathf
    {
        public static float E => 2.71828183f;
        public static float PI => Mathf.PI;
        public static float Tau => 6.283185307f;
        public static float Deg2Rad => Mathf.Deg2Rad;
        public static float Rad2Deg => Mathf.Rad2Deg;
        public static float Infinity => Mathf.Infinity;
        public static float NegativeInfinity => Mathf.NegativeInfinity;
        public static float Epsilon => Mathf.Epsilon;
        
        public static float Abs(float x) => math.abs(x);
        public static float Acos(float x) => math.acos(x);
        public static void Acosh(float x) => MathF.Acosh(x);
        public static float Asin(float x) => math.asin(x);
        public static float Asinh(float x) => MathF.Asinh(x);
        public static float Atan(float x) => math.atan(x);
        public static float Atan2(float y, float x) => math.atan2(y, x);
        public static float Atan2(float2 v) => math.atan2(v.y, v.x);
        public static float Atanh(float x) => MathF.Atanh(x);
        public static float Cbrt(float x) => MathF.Cbrt(x);
        public static float Ceiling(float x) => MathF.Ceiling(x);
        public static float Cos(float x) => math.cos(x);
        public static float Cosh(float x) => math.cosh(x);
        public static float Exp(float x) => math.exp(x);
        public static float Floor(float x) => math.floor(x);
        public static float FusedMultiplyAdd(float x, float y, float z) => math.mad(x, y, z);
        public static float IEEERemainder(float x, float y) => MathF.IEEERemainder(x, y);
        public static float IEEERemainder(float2 v) => MathF.IEEERemainder(v.x, v.y);
        public static float Log(float x) => math.log(x);
        public static float Log(float x, float y) => Mathf.Log(x, y);
        public static float Log(float2 v) => Mathf.Log(v.x, v.y);
        public static float Log10(float x) => math.log10(x);
        public static float Max(float x, float y) => math.max(x, y);
        public static float Max(float2 v) => math.max(v.x, v.y);
        public static float Min(float x, float y) => math.min(x, y);
        public static float Min(float2 v) => math.min(v.x, v.y);
        public static float Pow(float x, float y) => math.pow(x, y);
        public static float Pow(float2 v) => math.pow(v.x, v.y);
        public static float Round(float x, int digits, MidpointRounding midpointRounding) =>
            MathF.Round(x, digits, midpointRounding);
        public static float Sign(float x) => math.sign(x);
        public static float Sin(float x) => math.sin(x);
        public static SinCos SinCos(float x) => new SinCos(math.sin(x), math.cos(x));
        public static float Sinh(float x) => math.sinh(x);
        public static float Sqrt(float x) => math.sqrt(x);
        public static float Tan(float x) => math.tan(x);
        public static float Tanh(float x) => math.tanh(x);
        public static float Truncate(float x) => MathF.Truncate(x);
    }
}