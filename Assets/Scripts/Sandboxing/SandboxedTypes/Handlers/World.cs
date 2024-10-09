using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Game;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.Sandboxing.SandboxedTypes.Handlers
{
    public class World
    {
        private GameInstance gameInstance;
        private SandboxRestriction sandboxRestriction;

        public World() => throw new Exception("Cannot instantiate Players!");
        internal World(GameInstance gameInstance, SandboxRestriction sandboxRestriction)
        {
            this.gameInstance = gameInstance;
            this.sandboxRestriction = sandboxRestriction;
            Properties = new WorldProperties(gameInstance, sandboxRestriction != SandboxRestriction.Local);
        }
        
        public WorldProperties Properties { get; }

        public Item[] Children
        {
            get
            {
                if (sandboxRestriction == SandboxRestriction.LocalAvatar)
                    return Array.Empty<Item>();
                List<Item> children = new List<Item>();
                foreach (GameObject rootGameObject in gameInstance.loadedScene.GetRootGameObjects())
                {
                    bool hasPlayer = rootGameObject.GetComponent<LocalPlayer>() != null ||
                                     rootGameObject.GetComponent<NetPlayer>() != null;
                    if(hasPlayer) continue;
                    children.Add(new Item(rootGameObject.transform, false));
                }
                return children.ToArray();
            }
        }
        
        public Item GetItemInRoot(string name)
        {
            if(sandboxRestriction == SandboxRestriction.LocalAvatar) return null;
            Item[] children = Children;
            if (children.Count(x => x.Name == name) <= 0) return null;
            return children.First(x => x.Name == name);
        }

        public void SetSkyboxMaterial(string asset)
        {
            if(sandboxRestriction == SandboxRestriction.LocalAvatar) return;
            Object objectMaterial = SandboxTools.GetObjectFromWorldResource(asset, gameInstance);
            if (objectMaterial == null) return;
            Material material = (Material) objectMaterial;
            RenderSettings.skybox = material;
        }

        public void UpdateEnvironment()
        {
            if(sandboxRestriction == SandboxRestriction.LocalAvatar) return;
            DynamicGI.UpdateEnvironment();
        }

        [Obsolete("DuplicateItem is now Obsolete. Use Item.Duplicate instead")]
        public Item DuplicateItem(Item item, string name = "")
        {
            if(sandboxRestriction == SandboxRestriction.LocalAvatar) return null;
            if (item.IsReadOnly) return null;
            return item.Duplicate(name);
        }

        [Obsolete("UpdateWorldProperties is now Obsolete. Use World.Properties instead")]
        public void UpdateWorldProperties(WorldProperties worldProperties)
        {
        }
    }
}