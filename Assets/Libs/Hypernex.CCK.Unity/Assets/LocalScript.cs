using System;
using Hypernex.CCK.Unity.Scripting;
using UnityEngine;
#if UNITY_EDITOR
using TriInspector;
#endif

namespace Hypernex.CCK.Unity.Assets
{
#if UNITY_EDITOR
    [HideMonoScript]
#endif
    [Serializable]
    public class LocalScript : MonoBehaviour
    {
        public ModuleScript Script;
    }
}