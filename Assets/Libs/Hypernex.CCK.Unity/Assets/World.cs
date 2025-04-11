using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using TriInspector;
#endif

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
    [RequireComponent(typeof(AssetIdentifier))]
#if UNITY_EDITOR
    [HideMonoScript]
#endif
    public class World : MonoBehaviour
    {
#if UNITY_EDITOR
        [Title("World Settings")]
#endif
        public bool AllowRespawn = true;
        public float Gravity = -9.87f;
        public float JumpHeight = 1.0f;
        public float WalkSpeed = 3f;
        public float RunSpeed = 7f;
        public bool AllowRunning = true;
        public bool AllowScaling = true;
        public bool LockAvatarSwitching;
#if UNITY_EDITOR
        [ValidateInput(nameof(ValidateSpawnPoints))]
#endif
        public List<GameObject> SpawnPoints = new List<GameObject>();

#if UNITY_EDITOR
        [Title("Scripting")]
        [ValidateInput(nameof(ValidateNames))] 
#endif
        public List<ScriptAsset> ScriptAssets = new List<ScriptAsset>();

#if UNITY_EDITOR
        private TriValidationResult ValidateSpawnPoints()
        {
            if (SpawnPoints.Count <= 0)
                return TriValidationResult.Info("No SpawnPoints specified. This GameObject will be the default.");
            foreach (GameObject spawnPoint in SpawnPoints)
            {
                if(spawnPoint == null)
                    return TriValidationResult.Error("SpawnPoint cannot be null!");
            }
            return TriValidationResult.Valid;
        }

        private TriValidationResult ValidateNames()
        {
            string[] names = ScriptAssets.Select(x => x.AssetName).ToArray();
            foreach (ScriptAsset scriptAsset in ScriptAssets)
            {
                if(string.IsNullOrEmpty(scriptAsset.AssetName))
                    return TriValidationResult.Error("AssetName cannot be empty!");
                int c = 0;
                foreach (string s in names)
                {
                    if (scriptAsset.AssetName == s)
                        c++;
                }
                if(c > 1) return TriValidationResult.Error("AssetNames cannot be the same!");
            }
            return TriValidationResult.Valid;
        }
#endif
    }
}