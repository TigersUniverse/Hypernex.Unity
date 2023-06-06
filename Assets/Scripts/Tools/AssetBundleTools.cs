using HypernexSharp.APIObjects;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        
        public static string LoadSceneFromFile(string file)
        {
            AssetBundle loadedAssetBundle = AssetBundle.LoadFromFile(file);
            if (loadedAssetBundle != null)
            {
                string scenePath = loadedAssetBundle.GetAllScenePaths()[0];
                loadedAssetBundle.UnloadAsync(false);
                return scenePath;
            }
            return null;
        }

        public static Avatar LoadAvatarFromFile(string file)
        {
            AssetBundle loadedAssetBundle = AssetBundle.LoadFromFile(file);
            if (loadedAssetBundle != null)
            {
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
            loadedAssetBundle.UnloadAsync(false);
            return null;
        }
    }
}