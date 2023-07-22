using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HypernexSharp.APIObjects;
using UnityEngine;
using Avatar = Hypernex.CCK.Unity.Avatar;
using Logger = Hypernex.CCK.Logger;
using Object = UnityEngine.Object;

namespace Hypernex.Tools
{
    public class AssetBundleTools
    {
        public static BuildPlatform Platform
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        return BuildPlatform.Android;
                    default:
                        return BuildPlatform.Windows;
                }
            }
        }

        private static Dictionary<string, AssetBundle> cachedAssetBundles = new ();

        public static IEnumerator LoadSceneFromFile(string file, Action<string> r)
        {
            if (cachedAssetBundles.ContainsKey(file))
                r.Invoke(cachedAssetBundles[file].GetAllScenePaths()[0]);
            else
            {
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(file);
                yield return new WaitUntil(() => request.isDone);
                AssetBundle loadedAssetBundle = request.assetBundle;
                if (loadedAssetBundle != null)
                {
                    cachedAssetBundles.Add(file, loadedAssetBundle);
                    string scenePath = loadedAssetBundle.GetAllScenePaths()[0];
                    //loadedAssetBundle.UnloadAsync(false);
                    r.Invoke(scenePath);
                }
                else
                    r.Invoke(null);
            }
        }

        public static IEnumerator LoadAvatarFromFile(string file, Action<Avatar> r)
        {
            bool invoked = false;
            if (cachedAssetBundles.ContainsKey(file))
            {
                GameObject[] gameObjects = cachedAssetBundles[file].LoadAllAssets<GameObject>();
                foreach (GameObject obj in gameObjects)
                {
                    Avatar avatar = obj.GetComponent<Avatar>();
                    if (!invoked && avatar != null)
                    {
                        r.Invoke(avatar);
                        invoked = true;
                    }
                }
            }
            else
            {
#if UNITY_ANDROID
                MemoryStream ms = new MemoryStream();
                FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete);
                fs.CopyTo(ms);
                byte[] d = ms.ToArray();
                ms.Dispose();
                fs.Dispose();
                AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(d);
#else
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(file);
#endif
                yield return new WaitUntil(() => request.isDone);
                AssetBundle loadedAssetBundle = request.assetBundle;
                if (loadedAssetBundle != null)
                {
                    cachedAssetBundles.Add(file, loadedAssetBundle);
                    GameObject[] GameObjects = loadedAssetBundle.LoadAllAssets<GameObject>();
                    foreach (GameObject obj in GameObjects)
                    {
                        Avatar avatar = obj.GetComponent<Avatar>();
                        if (!invoked && avatar != null)
                        {
                            r.Invoke(avatar);
                            invoked = true;
                        }
                    }
                }
                else
                    r.Invoke(null);
            }
        }

        public static Avatar LoadAvatarFromFile(string file)
        {
            bool a = false;
            AssetBundle loadedAssetBundle;
            if (cachedAssetBundles.ContainsKey(file))
            {
                loadedAssetBundle = cachedAssetBundles[file];
                a = true;
            }
            else
                loadedAssetBundle = AssetBundle.LoadFromFile(file);
            if (loadedAssetBundle != null)
            {
                if(!a)
                    cachedAssetBundles.Add(file, loadedAssetBundle);
                Object[] loadedAssets = loadedAssetBundle.LoadAllAssets();
                foreach (Object loadedAsset in loadedAssets)
                {
                    if (loadedAsset is GameObject obj)
                    {
                        Avatar avatar = obj.GetComponent<Avatar>();
                        if (avatar != null)
                            return avatar;
                    }
                }
            }
            //loadedAssetBundle.UnloadAsync(false);
            return null;
        }

        internal static void UnloadAllAssetBundles()
        {
            foreach (KeyValuePair<string, AssetBundle> assetBundle in new Dictionary<string, AssetBundle>(cachedAssetBundles))
            {
                assetBundle.Value.Unload(true);
                cachedAssetBundles.Remove(assetBundle.Key);
            }
        }
    }
}