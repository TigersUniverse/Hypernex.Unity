using System;
#if UNITY_EDITOR
using TriInspector;
#endif
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
    public class ScriptAsset
    {
        public string AssetName;
#if UNITY_EDITOR
        [AssetsOnly]
#endif
        public Object Asset;
    }
}