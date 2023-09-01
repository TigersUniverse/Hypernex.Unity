using System;
using Hypernex.CCK.Unity;
using Hypernex.Game;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class LocalWorld
    {
        private static Object GetObjectFromWorldResource(string asset)
        {
            if (GameInstance.FocusedInstance == null)
                return null;
            foreach (ScriptAsset worldScriptAsset in GameInstance.FocusedInstance.World.ScriptAssets)
            {
                if (worldScriptAsset.AssetName == asset)
                    return worldScriptAsset.Asset;
            }
            return null;
        }
        
        [Obsolete("Use Item.Enabled instead")]
        public static bool GetItemActive(Item item) => item.t.gameObject.activeSelf;
        [Obsolete("Use Item.Enabled instead")]
        public static void SetItemActive(Item item, bool v) => item.t.gameObject.SetActive(v);

        public static Item GetItemInRoot(string name)
        {
            if (string.IsNullOrEmpty(name) || GameInstance.FocusedInstance == null)
                return null;
            foreach (GameObject rootGameObject in GameInstance.FocusedInstance.loadedScene.GetRootGameObjects())
                if (rootGameObject.name == name)
                    return new Item(rootGameObject.transform);
            return null;
        }

        public static void SetSkyboxMaterial(string asset)
        {
            Object objectMaterial = GetObjectFromWorldResource(asset);
            if (objectMaterial == null) return;
            Material material = (Material) objectMaterial;
            if(material == null) return;
            RenderSettings.skybox = material;
        }

        public static void UpdateEnvironment() => DynamicGI.UpdateEnvironment();
    }
}