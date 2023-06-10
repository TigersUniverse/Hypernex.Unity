using Hypernex.Game;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class LocalWorld
    {
        public static bool GetItemActive(Item item) => item.t.gameObject.activeSelf;
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
    }
}