using System;
using Hypernex.CCK.Unity;
using Hypernex.Game;
using Hypernex.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class LocalWorld
    {
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
            Object objectMaterial = SandboxTools.GetObjectFromWorldResource(asset);
            if (objectMaterial == null) return;
            Material material = (Material) objectMaterial;
            RenderSettings.skybox = material;
        }

        public static void UpdateEnvironment() => DynamicGI.UpdateEnvironment();

        public static Item DuplicateItem(Item item, string name = "")
        {
            Transform r = AnimationUtility.GetRootOfChild(item.t);
            if (r == null || r.GetComponent<LocalPlayer>() != null || r.GetComponent<NetPlayer>() != null)
                return null;
            Transform d = Object.Instantiate(item.t.gameObject).transform;
            if (!string.IsNullOrEmpty(name))
            {
                bool allow = true;
                if (d.parent == null)
                {
                    foreach (GameObject rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        if (rootGameObject.name == name)
                            allow = false;
                    }
                }
                else
                {
                    for (int i = 0; i < d.parent.childCount; i++)
                    {
                        Transform child = d.parent.GetChild(i);
                        if (child.name == name)
                            allow = false;
                    }
                }
                if (allow)
                    d.gameObject.name = name;
            }
            return new Item(d);
        }

        public static void UpdateWorldProperties(WorldProperties worldProperties)
        {
            if(GameInstance.FocusedInstance == null || GameInstance.FocusedInstance.World == null)
                return;
            World world = GameInstance.FocusedInstance.World;
            world.AllowRespawn = worldProperties.AllowRespawn;
            world.Gravity = worldProperties.Gravity;
            world.JumpHeight = worldProperties.JumpHeight;
            world.WalkSpeed = worldProperties.WalkSpeed;
            world.RunSpeed = worldProperties.RunSpeed;
            world.AllowRunning = worldProperties.AllowRunning;
            world.AllowScaling = worldProperties.AllowScaling;
            world.LockAvatarSwitching = worldProperties.LockAvatarSwitching;
        }
    }
}