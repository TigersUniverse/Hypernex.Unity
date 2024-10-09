using Hypernex.CCK.Unity;
using Hypernex.Game;
using Hypernex.Sandboxing.SandboxedTypes;
using UnityEngine;

namespace Hypernex.Sandboxing
{
    public static class SandboxTools
    {
        public static Object GetObjectFromWorldResource(string asset, GameInstance gameInstance)
        {
            if (gameInstance == null)
                return null;
            foreach (ScriptAsset worldScriptAsset in gameInstance.World.ScriptAssets)
            {
                if (worldScriptAsset.AssetName == asset)
                    return worldScriptAsset.Asset;
            }
            return null;
        }
    }
}