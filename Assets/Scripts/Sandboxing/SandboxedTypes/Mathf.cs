using System;
using Hypernex.Networking.Messages.Data;
using UnityEngine;

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
        
        public static float Abs(float x) => Mathf.Abs(x);
        public static float Acos(float x) => Mathf.Acos(x);
        public static void Acosh(float x) => MathF.Acosh(x);
        public static float Asin(float x) => Mathf.Asin(x);
        public static float Asinh(float x) => MathF.Asinh(x);
        public static float Atan(float x) => Mathf.Atan(x);
        public static float Atan2(float y, float x) => Mathf.Atan2(y, x);
        public static float Atan2(float2 v) => Mathf.Atan2(v.y, v.x);
        public static float Atanh(float x) => MathF.Atanh(x);
        public static float Cbrt(float x) => MathF.Cbrt(x);
        public static float Ceiling(float x) => MathF.Ceiling(x);
        public static float Cos(float x) => Mathf.Cos(x);
        public static float Cosh(float x) => MathF.Cosh(x);
        public static float Exp(float x) => Mathf.Exp(x);
        public static float Floor(float x) => Mathf.Floor(x);
        public static float IEEERemainder(float x, float y) => MathF.IEEERemainder(x, y);
        public static float IEEERemainder(float2 v) => MathF.IEEERemainder(v.x, v.y);
        public static float Log(float x) => Mathf.Log(x);
        public static float Log10(float x) => Mathf.Log10(x);
        public static float Max(float x, float y) => Mathf.Max(x, y);
        public static float Max(float2 v) => Mathf.Max(v.x, v.y);
        public static float Min(float x, float y) => Mathf.Min(x, y);
        public static float Min(float2 v) => Mathf.Min(v.x, v.y);
        public static float Pow(float x, float y) => Mathf.Pow(x, y);
        public static float Pow(float2 v) => Mathf.Pow(v.x, v.y);
        public static float Round(float x, int digits, MidpointRounding midpointRounding) =>
            MathF.Round(x, digits, midpointRounding);
        public static float Sign(float x) => Mathf.Sign(x);
        public static float Sin(float x) => Mathf.Sin(x);
        public static SinCos SinCos(float x) => new SinCos(Mathf.Sin(x), Mathf.Cos(x));
        public static float Sinh(float x) => MathF.Sinh(x);
        public static float Sqrt(float x) => Mathf.Sqrt(x);
        public static float Tan(float x) => Mathf.Tan(x);
        public static float Tanh(float x) => MathF.Tanh(x);
        public static float Truncate(float x) => MathF.Truncate(x);
    }
}