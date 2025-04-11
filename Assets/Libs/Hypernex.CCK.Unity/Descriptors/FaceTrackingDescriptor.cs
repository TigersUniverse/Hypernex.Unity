using System.Collections.Generic;
using Hypernex.CCK.Unity.Interaction;
using UnityEngine;
using Avatar = Hypernex.CCK.Unity.Assets.Avatar;

namespace Hypernex.CCK.Unity.Descriptors
{
    [RequireComponent(typeof(Avatar))]
    public class FaceTrackingDescriptor : MonoBehaviour
    {
        public List<SkinnedMeshRenderer> SkinnedMeshRenderers = new List<SkinnedMeshRenderer>();

        public int[] FaceValues = new int[(int) FaceExpressions.Max];
        public int[] ExtraEyeValues = new int[(int) ExtraEyeExpressions.Max];
    }
}