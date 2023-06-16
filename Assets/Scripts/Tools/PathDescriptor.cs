using UnityEngine;

namespace Hypernex.Tools
{
    public class PathDescriptor : MonoBehaviour
    {
        public Transform root;
        public Transform parent;
        public string path;

        private void Update()
        {
            if(root == null)
                return;
            if (transform.parent != parent)
            {
                parent = transform.parent;
                path = AnimationUtility.CalculateTransformPath(transform, root);
            }
        }
    }
}