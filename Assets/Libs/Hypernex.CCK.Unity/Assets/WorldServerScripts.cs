using System;
using Hypernex.CCK.Unity.Scripting;
using UnityEngine;
#if UNITY_EDITOR
using TriInspector;
using UnityEditor;
#endif

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
    [CreateAssetMenu(fileName = "World Server Scripts", menuName = "Hypernex/World/ServerScripts")]
#if UNITY_EDITOR
    [HideMonoScript]
    [DeclareHorizontalGroup("WorldServerScriptsGroup")]
#endif
    public class WorldServerScripts : ScriptableObject
    {
#if UNITY_EDITOR
        [ListDrawerSettings(AlwaysExpanded = true)]
#endif
        public ModuleScript[] ServerScripts;

#if UNITY_EDITOR && HYPERNEX_CCK_EMULATOR
        [NonSerialized] private bool isRunning;
        
        [Button("Test this Script")]
        [ValidateInput(nameof(IsTestLocal))]
        [Group("WorldServerScriptsGroup")]
        [HideIf(nameof(isRunning))]
        public void Button()
        {
            EditorPrefs.SetString("WorldServerScript", AssetDatabase.GetAssetPath(this));
        }

        [Button("Stop Testing all Scripts")]
        [ValidateInput(nameof(IsTestingAny))]
        [Group("WorldServerScriptsGroup")]
        [HideIf(nameof(isRunning))]
        public void DontTest()
        {
            EditorPrefs.DeleteKey("WorldServerScript");
        }

        private TriValidationResult IsTestLocal()
        {
            string a = EditorPrefs.GetString("WorldServerScript");
            string b = AssetDatabase.GetAssetPath(this);
            return a == b
                ? TriValidationResult.Info("You are testing this script.")
                : TriValidationResult.Warning("You are not testing this script!");
        }

        private TriValidationResult IsTestingAny()
        {
            bool t = EditorPrefs.HasKey("WorldServerScript");
            if(!t) return TriValidationResult.Error("You are not testing any ServerScripts!");
            WorldServerScripts w = AssetDatabase.LoadAssetAtPath<WorldServerScripts>(EditorPrefs.GetString("WorldServerScript"));
            if(w == null) return TriValidationResult.Error("You are not testing any ServerScripts!");
            return TriValidationResult.Info($"You are testing {w.name}");
        }

        private void UpdateState(PlayModeStateChange state) => isRunning = state == PlayModeStateChange.EnteredPlayMode;

        private void OnEnable() => EditorApplication.playModeStateChanged += UpdateState;
        private void OnDisable() => EditorApplication.playModeStateChanged -= UpdateState;
#endif
    }
}