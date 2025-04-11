using Hypernex.CCK.Unity.Descriptors;

namespace Hypernex.CCK.Unity.Internals
{
    public static class Utils
    {
        public static int GetIndex(this BlendshapeDescriptor descriptor, BlendshapeDescriptor[] descriptors)
        {
            for (int i = 0; i < descriptors.Length; i++)
            {
                BlendshapeDescriptor match = descriptors[i];
                if(match.MatchString != descriptor.MatchString) continue;
                return i;
            }
            return -1;
        }
    }
}