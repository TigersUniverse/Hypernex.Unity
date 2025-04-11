using System.Collections.Generic;
using Hypernex.CCK.Unity.Interaction;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Hypernex.CCK.Unity.Internals
{
    public struct AnimatorPlayable
    {
        public CustomPlayableAnimator CustomPlayableAnimator;
        public PlayableGraph PlayableGraph;
        public AnimatorControllerPlayable AnimatorControllerPlayable;
        public PlayableOutput PlayableOutput;
        public List<AnimatorControllerParameter> AnimatorControllerParameters;
    }
}