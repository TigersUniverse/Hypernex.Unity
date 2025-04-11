using System;
using System.Collections;
using System.Collections.Generic;
using Hypernex.Game;
using HypernexSharp.APIObjects;
using UnityEngine;
using Avatar = Hypernex.CCK.Unity.Assets.Avatar;

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

        private static Dictionary<string, Avatar> cachedAssetBundlesAvatars = new ();

        private static IEnumerator UnloadWorld(AssetBundle assetBundle)
        {
            AssetBundleUnloadOperation unloadOperation = assetBundle.UnloadAsync(true);
            yield return new WaitUntil(() => unloadOperation.isDone);
        }

        public static IEnumerator LoadSceneFromFile(string file, Action<string> r, GameInstance gameInstance)
        {
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(file);
            yield return new WaitUntil(() => request.isDone);
            AssetBundle loadedAssetBundle = request.assetBundle;
            if (loadedAssetBundle != null)
            {
                string scenePath = loadedAssetBundle.GetAllScenePaths()[0];
                r.Invoke(scenePath);
                //gameInstance.OnDisconnect += () => CoroutineRunner.Instance.Run(UnloadWorld(loadedAssetBundle));
            }
            else
                r.Invoke(null);
            AssetBundleUnloadOperation unloadOperation = loadedAssetBundle.UnloadAsync(false);
            yield return new WaitUntil(() => unloadOperation.isDone);
        }

        public static IEnumerator LoadAvatarFromFile(string file, Action<Avatar> r)
        {
            bool invoked = false;
            if (cachedAssetBundlesAvatars.ContainsKey(file))
            {
                Avatar avatar = cachedAssetBundlesAvatars[file];
                r.Invoke(avatar);
            }
            else
            {
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(file);
                yield return new WaitUntil(() => request.isDone);
                AssetBundle loadedAssetBundle = request.assetBundle;
                if (loadedAssetBundle != null)
                {
                    GameObject[] GameObjects = loadedAssetBundle.LoadAllAssets<GameObject>();
                    foreach (GameObject obj in GameObjects)
                    {
                        Avatar avatar = obj.GetComponent<Avatar>();
                        if (!invoked && avatar != null)
                        {
                            cachedAssetBundlesAvatars.Add(file, avatar);
                            r.Invoke(avatar);
                            invoked = true;
                        }
                    }
                    AssetBundleUnloadOperation unloadOperation = loadedAssetBundle.UnloadAsync(false);
                    yield return new WaitUntil(() => unloadOperation.isDone);
                }
                else
                    r.Invoke(null);
            }
        }

        internal static void UnloadAllAssetBundles()
        {
            /*foreach (KeyValuePair<string, AssetBundle> assetBundle in new Dictionary<string, AssetBundle>(cachedAssetBundles))
            {
                assetBundle.Value.Unload(true);
                cachedAssetBundles.Remove(assetBundle.Key);
            }*/
        }
    }
}