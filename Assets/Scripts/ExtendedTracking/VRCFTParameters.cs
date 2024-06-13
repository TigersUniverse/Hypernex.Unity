using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Hypernex.Game.Avatar;
using HypernexSharp.APIObjects;
using UnityEngine;
using VRCFaceTracking;
using VRCFaceTracking.Core.Models.Osc.FileBased;
using VRCFaceTracking.Core.Models.ParameterDefinition.FileBased;
using VRCFaceTracking.Core.OSC.DataTypes;
using VRCFaceTracking.Core.Params;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.DataTypes;
using Object = System.Object;
using Vector2 = VRCFaceTracking.Core.Types.Vector2;

namespace Hypernex.ExtendedTracking
{
    public static class VRCFTParameters
    {
        public static bool UseBinary { get; set; } = true;
        
        public static void UpdateParameters(AvatarMeta avatarMeta, LocalAvatarCreator avatarCreator, ParameterVersion parameterVersion = ParameterVersion.Both)
        {
            List<AvatarConfigFileParameter> parameters = new List<AvatarConfigFileParameter>();
            avatarCreator.Parameters.Where(x => x.type != AnimatorControllerParameterType.Trigger).ToList().ForEach(
                parameter =>
                {
                    if (parameters.Count(x => x.name == parameter.name) > 0) return;
                    AvatarConfigFileParameter p = new AvatarConfigFileParameter
                    {
                        name = parameter.name,
                        input = new AvatarConfigFileIODef
                        {
                            address = "/avatar/parameters/" + parameter.name
                        }
                    };
                    switch (parameter.type)
                    {
                        case AnimatorControllerParameterType.Int:
                            p.input.type = "Int";
                            break;
                        case AnimatorControllerParameterType.Bool:
                            p.input.type = "Bool";
                            break;
                        case AnimatorControllerParameterType.Float:
                            p.input.type = "Float";
                            break;
                    }

                    parameters.Add(p);
                });
            AvatarConfigFile avatarConfigSpec = new AvatarConfigFile
            {
                id = avatarMeta.Id,
                name = avatarMeta.Name,
                parameters = parameters.ToArray()
            };
            FaceTrackingManager.CustomFaceExpressions.RemoveAll(x => x is VRCFTProgrammableExpression);
            Parameter[] parametersToPullFrom;
            switch (parameterVersion)
            {
                case ParameterVersion.v1:
                    parametersToPullFrom = UnifiedTracking.AllParameters_v1;
                    break;
                case ParameterVersion.v2:
                    parametersToPullFrom = UnifiedTracking.AllParameters_v2;
                    break;
                case ParameterVersion.Both:
                    parametersToPullFrom =
                        UnifiedTracking.AllParameters_v2.Concat(UnifiedTracking.AllParameters_v1).ToArray();
                    break;
                default:
                    throw new Exception("Unknown ParameterVersion");
            }
            List<Parameter> paramList = new List<Parameter>();
            foreach (Parameter parameter in parametersToPullFrom)
                paramList.AddRange(parameter.ResetParam(avatarConfigSpec.Parameters));
            GetParameters(paramList.ToArray())
                .ForEach(parameter => FaceTrackingManager.CustomFaceExpressions.Add(parameter));
        }
        
        private static List<VRCFTProgrammableExpression> GetParameters(Parameter[] parametersToPullFrom)
        {
            List<VRCFTProgrammableExpression> expressions = new();
            foreach (Parameter vrcftParameter in parametersToPullFrom)
            {
                Type parameterType = GetRootTypeNoAbstractParameter(vrcftParameter.GetType());
                if (parameterType == typeof(BaseParam<float>))
                {
                    BaseParam<float> paramLiteral = (BaseParam<float>) vrcftParameter;
                    (Func<string>, Func<UnifiedTrackingData, float>) paramValue = GetBaseParamValue(paramLiteral);
                    expressions.Add(new VRCFTProgrammableExpression(paramValue.Item1, paramValue.Item2));
                }
                else if (parameterType == typeof(BaseParam<bool>))
                {
                    BaseParam<bool> paramLiteral = (BaseParam<bool>) vrcftParameter;
                    (Func<string>, Func<UnifiedTrackingData, bool>) paramValue = GetBaseParamValue(paramLiteral);
                    expressions.Add(new VRCFTProgrammableExpression(paramValue.Item1,
                        data => paramValue.Item2.Invoke(data) ? 1.0f : 0.0f));
                }
                else if (parameterType == typeof(BaseParam<Vector2>))
                {
                    BaseParam<Vector2> paramLiteral = (BaseParam<Vector2>) vrcftParameter;
                    (Func<string>, Func<UnifiedTrackingData, Vector2>) paramValue = GetBaseParamValue(paramLiteral);
                    expressions.Add(new VRCFTProgrammableExpression(() => paramValue.Item1.Invoke() + "X",
                        data => paramValue.Item2.Invoke(data).x));
                    expressions.Add(new VRCFTProgrammableExpression(() => paramValue.Item1.Invoke() + "Y",
                        data => paramValue.Item2.Invoke(data).y));
                }
                else if (UseBinary && parameterType == typeof(BinaryBaseParameter))
                {
                    BinaryBaseParameter paramLiteral = (BinaryBaseParameter) vrcftParameter;
                    foreach ((Func<string>, Func<UnifiedTrackingData, bool>) valueTuple in
                             GetBinaryBaseParamValue(paramLiteral))
                        expressions.Add(new VRCFTProgrammableExpression(valueTuple.Item1,
                            data => valueTuple.Item2.Invoke(data) ? 1.0f : 0.0f));
                }
            }
            foreach (Parameter vrcftParameter in parametersToPullFrom)
            {
                Type parameterType = GetRootTypeNoAbstractParameter(vrcftParameter.GetType());
                if (parameterType != typeof(EParam)) continue;
                EParam paramLiteral = (EParam) vrcftParameter;
                bool exists = false;
                foreach ((string, Parameter) parameter in paramLiteral.GetParamNames())
                {
                    if (expressions.Select(x => x.Name).Contains(parameter.Item1))
                    {
                        exists = true;
                        break;
                    }
                    foreach ((string, Parameter) valueTuple in parameter.Item2.GetParamNames())
                    {
                        if (expressions.Select(x => x.Name).Contains(valueTuple.Item1))
                        {
                            exists = true;
                            break;
                        }
                        if (expressions.Select(x => x.Name).Contains(valueTuple.Item2.GetParamNames()[0].paramName))
                        {
                            exists = true;
                            break;
                        }
                    }
                }
                if(exists) continue;
                foreach ((Func<string>, Func<UnifiedTrackingData, float>) valueTuple in GetEParamValue(paramLiteral))
                    expressions.Add(new VRCFTProgrammableExpression(valueTuple.Item1, valueTuple.Item2));
            }
            return expressions;
        }

        private static Type GetRootTypeNoAbstractParameter(Type derivedType)
        {
            Type baseType = derivedType;
            Type? lastType = derivedType;
            while (lastType != null && lastType != typeof(Parameter) && lastType != typeof(Object))
            {
                baseType = lastType;
                lastType = baseType.BaseType;
            }
            return baseType;
        }

        private static (Func<string>, Func<UnifiedTrackingData, T>) GetBaseParamValue<T>(BaseParam<T> baseParam) where T : struct
        {
            Type paramType = GetRootTypeNoAbstractParameter(baseParam.GetType());
            Func<UnifiedTrackingData, T> getValueFunc =
                (Func<UnifiedTrackingData, T>) paramType.GetField("_getValueFunc",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(baseParam);
            return (() => baseParam.GetParamNames()[0].Item1, getValueFunc);
        }
        
        private static (Func<string>, Func<UnifiedTrackingData, bool>)[] GetBinaryBaseParamValue(BinaryBaseParameter baseParam)
        {
            List<(Func<string>, Func<UnifiedTrackingData, bool>)> binaryParameters = new();
            (string, Parameter)[] paramNames = baseParam.GetParamNames();
            foreach ((string, Parameter) valueTuple in paramNames)
            {
                BaseParam<bool> paramLiteral = (BaseParam<bool>) valueTuple.Item2;
                binaryParameters.Add((() => paramLiteral.GetParamNames()[0].Item1,
                    (Func<UnifiedTrackingData, bool>) paramLiteral.GetType()
                        .GetField("_getValueFunc", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .GetValue(paramLiteral)));
            }
            return binaryParameters.ToArray();
        }
        
        private static (Func<string>, Func<UnifiedTrackingData, float>)[] GetEParamValue(EParam eParam)
        {
            List<(Func<string>, Func<UnifiedTrackingData, float>)> eParameters = new();
            (string, Parameter)[] paramNames = eParam.GetParamNames();
            foreach ((string, Parameter) valueTuple in paramNames)
            {
                Type paramLiteralType = GetRootTypeNoAbstractParameter(valueTuple.Item2.GetType());
                if (paramLiteralType == typeof(BaseParam<float>))
                {
                    (Func<string>, Func<UnifiedTrackingData, float>) baseParamFunc =
                        GetBaseParamValue((BaseParam<float>) valueTuple.Item2);
                    eParameters.Add(baseParamFunc);
                }
                else if (UseBinary && paramLiteralType == typeof(BinaryBaseParameter))
                {
                    BinaryBaseParameter binaryBaseParameter = (BinaryBaseParameter) valueTuple.Item2;
                    foreach ((Func<string>, Func<UnifiedTrackingData, bool>) binaryTuple in GetBinaryBaseParamValue(
                                 binaryBaseParameter))
                        eParameters.Add((binaryTuple.Item1, data => binaryTuple.Item2.Invoke(data) ? 1.0f : 0.0f));
                }
            }
            return eParameters.ToArray();
        }
        
        private class VRCFTProgrammableExpression : ICustomFaceExpression
        {
            private string? lastRegexName;
            private Regex? lastRegex;

            private Regex Regex
            {
                get
                {
                    if (lastRegexName == null) lastRegexName = Name;
                    if (lastRegex == null || lastRegexName != Name)
                    {
                        lastRegexName = Name;
                        lastRegex = new Regex(@"(?<!(v\d+))(/" + lastRegexName + ")$|^(" + lastRegexName + ")$");
                        matches.Clear();
                    }
                    return lastRegex;
                }
            }
            
            private Dictionary<string, bool> matches = new();

            private Func<string> getNameFunc;
            private Func<UnifiedTrackingData, float> getWeightFunc;

            public VRCFTProgrammableExpression(Func<string> getNameFunc, Func<UnifiedTrackingData, float> getWeightFunc)
            {
                this.getNameFunc = getNameFunc;
                this.getWeightFunc = getWeightFunc;
            }

            public string Name => getNameFunc.Invoke();
            public float GetWeight(UnifiedTrackingData unifiedTrackingData) => getWeightFunc.Invoke(unifiedTrackingData);

            public bool IsMatch(string parameterName)
            {
                bool match;
                if (!matches.TryGetValue(parameterName, out match))
                {
                    match = Regex.IsMatch(parameterName);
                    matches.Add(parameterName, match);
                }
                return match;
            }
        }
        
        public enum ParameterVersion
        {
            [Obsolete] v1,
            v2,
            Both
        }
    }
}