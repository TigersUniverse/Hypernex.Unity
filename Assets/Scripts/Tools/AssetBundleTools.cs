using System.Collections.Generic;
using HypernexSharp.APIObjects;
using UnityEngine;
using Avatar = Hypernex.CCK.Unity.Avatar;

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

        private static List<AssetBundle> cachedAssetBundles = new ();

        public static string LoadSceneFromFile(string file)
        {
            AssetBundle loadedAssetBundle = AssetBundle.LoadFromFile(file);
            if (loadedAssetBundle != null)
            {
                cachedAssetBundles.Add(loadedAssetBundle);
                string scenePath = loadedAssetBundle.GetAllScenePaths()[0];
                //loadedAssetBundle.UnloadAsync(false);
                return scenePath;
            }
            return null;
        }

        public static Avatar LoadAvatarFromFile(string file)
        {
            AssetBundle loadedAssetBundle = AssetBundle.LoadFromFile(file);
            if (loadedAssetBundle != null)
            {
                cachedAssetBundles.Add(loadedAssetBundle);
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
            foreach (AssetBundle assetBundle in new List<AssetBundle>(cachedAssetBundles))
            {
                assetBundle.Unload(true);
                cachedAssetBundles.Remove(assetBundle);
            }
        }
    }
}