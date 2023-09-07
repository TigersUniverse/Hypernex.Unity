using Hypernex.CCK.Unity;
using Hypernex.Game;
using UnityEngine;

namespace Hypernex.Sandboxing
{
    public static class SandboxTools
    {
        public static Object GetObjectFromWorldResource(string asset)
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
    }
}