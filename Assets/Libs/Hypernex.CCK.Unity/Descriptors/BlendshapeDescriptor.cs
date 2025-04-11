using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.CCK.Unity.Descriptors
{
    [Serializable]
    public class BlendshapeDescriptor
    {
        public string MatchString;
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public int BlendshapeIndex;

        public void SetWeight(float weight) => SkinnedMeshRenderer.SetBlendShapeWeight(BlendshapeIndex, weight);

        public static int GetIndex(BlendshapeDescriptor[] blendshapes, BlendshapeDescriptor blendshapeDescriptor)
        {
            if (blendshapeDescriptor == null)
                return 0;
            for (int i = 0; i < blendshapes.Length; i++)
            {
                if (blendshapes[i].SkinnedMeshRenderer == blendshapeDescriptor.SkinnedMeshRenderer &&
                    blendshapes[i].BlendshapeIndex == blendshapeDescriptor.BlendshapeIndex)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        public static BlendshapeDescriptor GetDescriptor(BlendshapeDescriptor[] blendshapes, int[] matchArray, int index)
        {
            if (matchArray == null || index < 0 || index >= matchArray.Length)
                return null;
            int selectedIndex = matchArray[index];
            if (selectedIndex <= 0 || selectedIndex - 1 >= blendshapes.Length)
                return null;
            return blendshapes[selectedIndex - 1];
        }

        public static BlendshapeDescriptor[] GetAllDescriptors(params SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            List<BlendshapeDescriptor> descriptors = new List<BlendshapeDescriptor>();
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];
                Mesh mesh = skinnedMeshRenderer.sharedMesh;
                int count = mesh.blendShapeCount;
                for (int j = 0; j < count; j++)
                {
                    BlendshapeDescriptor descriptor = new BlendshapeDescriptor();
                    descriptor.BlendshapeIndex = j;
                    descriptor.SkinnedMeshRenderer = skinnedMeshRenderer;
                    descriptor.MatchString =
                        $"{skinnedMeshRenderer.gameObject.name} [{i}] - {mesh.GetBlendShapeName(j)} [{j}]";
                    descriptors.Add(descriptor);
                }
            }
            return descriptors.ToArray();
        }

        public static Dictionary<SkinnedMeshRenderer, HashSet<int>> GetUsedBlendshapes(
            BlendshapeDescriptor[] allDescriptors, params int[][] matchArrays)
        {
            Dictionary<SkinnedMeshRenderer, HashSet<int>> used = new Dictionary<SkinnedMeshRenderer, HashSet<int>>();
            foreach (var matchArray in matchArrays)
            {
                for (int i = 0; i < matchArray.Length; i++)
                {
                    int selectedIndex = matchArray[i];
                    if (selectedIndex <= 0 || selectedIndex - 1 >= allDescriptors.Length) continue;
                    BlendshapeDescriptor descriptor = allDescriptors[selectedIndex - 1];
                    if (descriptor?.SkinnedMeshRenderer == null) continue;
                    if (!used.TryGetValue(descriptor.SkinnedMeshRenderer, out HashSet<int> set))
                        used[descriptor.SkinnedMeshRenderer] = set = new HashSet<int>();
                    set.Add(descriptor.BlendshapeIndex);
                }
            }
            return used;
        }
    }
}