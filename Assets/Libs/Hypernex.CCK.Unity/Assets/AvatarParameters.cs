using System;
using UnityEngine;

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
    [CreateAssetMenu(fileName = "Avatar Parameters", menuName = "Hypernex/Avatars/Parameters")]
    public class AvatarParameters : ScriptableObject
    {
        public AvatarParameter[] Parameters = Array.Empty<AvatarParameter>();
    }

    [Serializable]
    public class AvatarParameter
    {
        public string ParameterName;
        public AnimatorControllerParameterType ParameterType;
        
        public bool DefaultBoolValue;
        public float DefaultFloatValue;
        public int DefaultIntValue;

        public bool Saved;

        public bool IsNetworked;
    }
}