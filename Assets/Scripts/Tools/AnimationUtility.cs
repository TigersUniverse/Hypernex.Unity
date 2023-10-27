using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools
{
    public static class AnimationUtility
    {
        public static Transform GetObjectFromRoot(string path, Scene? s = null)
        {
            if (s == null)
                s = SceneManager.GetActiveScene();
            GameObject[] rootGameObjects = s.Value.GetRootGameObjects();
            if (string.IsNullOrEmpty(path))
            {
                //Logger.CurrentLogger.Log("Empty Path");
                return null;
            }
            if (!path.Contains('/'))
                foreach (GameObject rootGameObject in s.Value.GetRootGameObjects())
                    if (rootGameObject.name == path)
                    {
                        //Logger.CurrentLogger.Log("Found Root Match of " + rootGameObject.name);
                        return rootGameObject.transform;
                    }
            string p = path;
            if (p[0] == '/')
                p = path.Substring(1, p.Length - 1);
            if (p[0] == '/')
            {
                //Logger.CurrentLogger.Log("Continuous Slash");
                return null;
            }
            string[] pathSplit = p.Split('/');
            for (int i = 0; i < rootGameObjects.Length; i++)
            {
                GameObject item = rootGameObjects[i];
                if (item.name == pathSplit[0])
                {
                    string newPath = String.Empty;
                    int x = 0;
                    foreach (string e in pathSplit)
                    {
                        if (x != 0)
                            newPath += e + '/';
                        x++;
                    }
                    newPath = newPath.Substring(0, newPath.Length - 1);
                    Transform h = item.transform.Find(newPath);
                    if (h == null)
                    {
                        //Logger.CurrentLogger.Log("Path to h was null " + newPath + " , " + p);
                        return null;
                    }
                    //Logger.CurrentLogger.Log("Found Child match of " + h.name);
                    return h;
                }
            }
            //Logger.CurrentLogger.Log("Couldn't find match");
            return null;
        }
        
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

        public static Transform GetLowestObject(Scene scene)
        {
            Transform lowestPoint = null;
            foreach (GameObject rootGameObject in scene.GetRootGameObjects())
            {
                Transform[] ts = rootGameObject.GetComponentsInChildren<Transform>(true);
                foreach (Transform transform in ts)
                {
                    if (lowestPoint == null)
                        lowestPoint = transform;
                    else if (transform.position.y < lowestPoint.position.y)
                        lowestPoint = transform;
                }
            }
            return lowestPoint;
        }

        public static bool IsChildOfTransform(Transform child, Transform t)
        {
            Transform parent = child.parent;
            if (parent == null)
                return false;
            while (parent != null)
            {
                if (parent == t)
                    return true;
                parent = parent.parent;
            }
            return false;
        }

#if DYNAMIC_BONE
        public static bool IsChildOfExclusion(List<Transform> exclusions, Transform bone)
        {
            foreach (Transform exclusion in exclusions)
            {
                foreach (Transform transform in exclusion.GetComponentsInChildren<Transform>(true))
                {
                    if (transform == bone)
                        return true;
                }
            }
            return false;
        }
#endif
    }
}