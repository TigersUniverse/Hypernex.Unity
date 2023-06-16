using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.Tools
{
    public static class AnimationUtility
    {
        public static Transform GetRootOfChild(Transform child)
        {
            if (child.parent == null)
                return child;
            Transform nextParent = child.parent;
            while (nextParent.parent != null)
                nextParent = nextParent.parent;
            return nextParent;
        }
        
        public static string CalculateTransformPath(Transform child, Transform root)
        {
            List<string> parents = new(){child.name};
            Transform nextParent = child.parent;
            while (nextParent != null && nextParent != root)
            {
                parents.Add(nextParent.name);
                nextParent = nextParent.parent;
            }
            string s = String.Empty;
            parents.Reverse();
            foreach (string parent in parents)
                s += parent + '/';
            s = s.Remove(s.Length - 1, 1);
            return s;
        }
    }
}