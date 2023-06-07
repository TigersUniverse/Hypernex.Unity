using Hypernex.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class LocalWorld
    {
        private static Scene s = GameInstance.FocusedInstance.loadedScene;
        
        public static bool GetItemActive(Item item) => item.t.gameObject.activeSelf;
        public static void SetItemActive(Item item, bool v) => item.t.gameObject.SetActive(v);

        public static Item GetItemInRoot(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            foreach (GameObject rootGameObject in s.GetRootGameObjects())
                if (rootGameObject.name == name)
                    return new Item(rootGameObject.transform);
            return null;
        }
    }
}