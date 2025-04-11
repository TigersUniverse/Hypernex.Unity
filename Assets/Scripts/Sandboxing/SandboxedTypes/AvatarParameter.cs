using System;
using System.Linq;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game.Avatar;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class AvatarParameter
    {
        private AvatarCreator avatarCreator;
        private AnimatorPlayable animatorPlayable;
        private AnimatorControllerParameter parameter;
        private bool allowWrite;

        public AvatarParameter() => throw new Exception("Cannot instantiate AvatarParameter");

        internal AvatarParameter(AvatarCreator ac, AnimatorPlayable a, AnimatorControllerParameter p, bool aw)
        {
            avatarCreator = ac;
            animatorPlayable = a;
            parameter = p;
            allowWrite = aw;
        }

        public string Name => parameter.name;

        public AvatarParameterType Type
        {
            get
            {
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        return AvatarParameterType.Bool;
                    case AnimatorControllerParameterType.Int:
                        return AvatarParameterType.Int;
                    case AnimatorControllerParameterType.Float:
                        return AvatarParameterType.Float;
                    default:
                        throw new Exception("Unsupported Trigger parameter");
                }
            }
        }

        public bool IsVisible => avatarCreator.Avatar.Parameters != null &&
                                 avatarCreator.Avatar.Parameters.Parameters.Count(x =>
                                     x.ParameterName == parameter.name && x.ParameterType == parameter.type) > 0;

        public object GetValue() => avatarCreator.GetParameter(parameter.name, animatorPlayable.CustomPlayableAnimator);

        public void SetValue(bool value)
        {
            if(!allowWrite)
               return;
            avatarCreator.SetParameter(parameter.name, value, animatorPlayable.CustomPlayableAnimator);
        }
        
        public void SetValue(int value)
        {
            if(!allowWrite)
                return;
            avatarCreator.SetParameter(parameter.name, value, animatorPlayable.CustomPlayableAnimator);
        }
        
        public void SetValue(float value)
        {
            if(!allowWrite)
                return;
            avatarCreator.SetParameter(parameter.name, value, animatorPlayable.CustomPlayableAnimator);
        }
    }
}