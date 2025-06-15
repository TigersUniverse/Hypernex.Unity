using Hypernex.CCK.Unity.Assets;
using Hypernex.Game;
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