using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Hypernex.Editor.BuildTools
{
    public class BuildWindow : EditorWindow
    {
        private static BuildWindow Instance { get; set; }

        private readonly string[] PlatformOptions =
        {
            "Windows",
            "macOS",
            "Linux",
            "iOS",
            "Android"
        };
        private int selectedBuildPlatform;
        
        [MenuItem("Hypernex/BuildTools")]
        private static void Show()
        {
            Instance = GetWindow<BuildWindow>();
            Instance.titleContent = new GUIContent("BuildTools");
            // Initialize platform
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    Instance.selectedBuildPlatform = 0;
                    break;
                case BuildTarget.StandaloneOSX:
                    Instance.selectedBuildPlatform = 1;
                    break;
                case BuildTarget.EmbeddedLinux:
                case BuildTarget.LinuxHeadlessSimulation:
                    Instance.selectedBuildPlatform = 2;
                    break;
                case BuildTarget.iOS:
                    Instance.selectedBuildPlatform = 3;
                    break;
                case BuildTarget.Android:
                    Instance.selectedBuildPlatform = 4;
                    break;
                default:
                    Instance.selectedBuildPlatform = -1;
                    break;
            }
        }

        private void RenderBuildPlatform()
        {
            GUILayout.Label("Build Target", EditorStyles.largeLabel);
            int previousSelectedPlatform = selectedBuildPlatform;
            if(selectedBuildPlatform > -1)
                selectedBuildPlatform = EditorGUILayout.Popup("Platform", selectedBuildPlatform, PlatformOptions, new GUIStyle(EditorStyles.popup));
            else
            {
                // TODO: prompt switching of build targets
                GUILayout.Label($"Unknown build target {EditorUserBuildSettings.activeBuildTarget}. Please switch to a valid build target to continue");
                return;
            }
            if (previousSelectedPlatform != selectedBuildPlatform)
            {
                // TODO: Update build platform
            }
        }

        private void RenderLibraries()
        {
            GUILayout.Label("Additional Libraries", EditorStyles.largeLabel);
            if (!LibrarySupport.IsVLCPresent())
            {
                GUILayout.Label("VLC is not installed!");
#if VLC
                LibrarySupport.RemoveScriptingDefineSymbol("VLC");
#endif
            }
            else
            {
                GUILayout.Label("VLC is installed!");
#if !VLC
                LibrarySupport.AddScriptingDefineSymbol("VLC");
#endif
            }
            if (LibrarySupport.IsMagicaPresent())
            {
                GUILayout.Label("MagicaCloth2 is installed!");
#if !MAGICACLOTH2
                LibrarySupport.AddScriptingDefineSymbol("MAGICACLOTH2");
#endif
            }
            else
            {
                GUILayout.Label("MagicaCloth2 is not installed!");
#if MAGICACLOTH2
                LibrarySupport.RemoveScriptingDefineSymbol("MAGICACLOTH2");
#endif
            }
        }

        private bool? DoesOpenXRExist()
        {
            XRGeneralSettings settings =
                XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(EditorUserBuildSettings
                    .selectedBuildTargetGroup);
            if (settings == null || settings.AssignedSettings == null) return null;
            foreach (XRLoader loader in settings.AssignedSettings.activeLoaders)
            {
                if (loader != null && loader.GetType().Name.Contains("OpenXR"))
                    return true;
            }
            return false;
        }

        private void ToggleOpenXR(bool val)
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            XRManagerSettings settings = XRGeneralSettingsPerBuildTarget
                .XRGeneralSettingsForBuildTarget(buildTargetGroup)?.AssignedSettings;
            if (settings == null) return;
            if (val)
            {
                XRPackageMetadataStore.AssignLoader(settings, typeof(OpenXRLoader).FullName, buildTargetGroup);
            }
            else
            {
                XRPackageMetadataStore.RemoveLoader(settings, typeof(OpenXRLoader).FullName, buildTargetGroup);
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private void RenderOpenXR()
        {
            bool? openXRLoaded = DoesOpenXRExist();
            if (openXRLoaded == null)
            {
                GUILayout.Label("OpenXR Configuration", EditorStyles.largeLabel);
                GUILayout.Label("Error in XRGeneralSettings!");
                return;
            }
#if UNITY_ANDROID
            GUILayout.Label("OpenXR Configuration", EditorStyles.largeLabel);
            GUILayout.Label($"Current XR Build Configuration: {(openXRLoaded.Value ? "XR Enabled" : "XR Disabled")}");
            if (openXRLoaded.Value)
            {
                EditorGUILayout.HelpBox(
                                    "Your Android app will only work on supported XR platforms. It will NOT launch on a mobile device.",
                                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Your Android app will only work on mobile devices. It will NOT launch on an XR device.",
                    MessageType.Info);
            }
            GUILayout.Label("How would you like to build your Android app?");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable XR"))
                ToggleOpenXR(true);
            if (GUILayout.Button("Disable XR"))
                ToggleOpenXR(false);
            GUILayout.EndHorizontal();
#elif UNITY_IOS
            if (openXRLoaded.Value)
            {
                GUILayout.Label("OpenXR Configuration", EditorStyles.largeLabel);
                GUILayout.Label("OpenXR is not supported on iOS. Would you like to disable it?");
                if(GUILayout.Button("Disable XR"))
                    ToggleOpenXR(false);
            }
#else
            if (!openXRLoaded.Value)
            {
                GUILayout.Label("OpenXR Configuration", EditorStyles.largeLabel);
                GUILayout.Label("It looks like XR was not loaded. Would you like to enable it?");
                if(GUILayout.Button("Enable XR"))
                    ToggleOpenXR(true);
            }
#endif
        }

        private void Build()
        {
            Assembly assembly = typeof(BuildPlayerWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.BuildPlayerWindow");
            MethodInfo method = type.GetMethod("CallBuildMethods", BindingFlags.NonPublic | BindingFlags.Static);
            method!.Invoke(null, new object[2] {true, BuildOptions.ShowBuiltPlayer | BuildOptions.CleanBuildCache});
        }

        private void RenderBuild()
        {
            GUILayout.Label("Ready to build?", EditorStyles.largeLabel);
            bool errors = EditorUtility.scriptCompilationFailed;
            if (errors)
            {
                EditorGUILayout.HelpBox("Please fix compilation errors before building!", MessageType.Error);
                GUI.enabled = false;
            }
            if (GUILayout.Button("Build!"))
            {
                if(errors) return;
                Build();
            }
            GUI.enabled = true;
        }

        private void OnGUI()
        {
            RenderBuildPlatform();
            EditorGUILayout.Space();
            RenderLibraries();
            EditorGUILayout.Space();
            RenderOpenXR();
            EditorGUILayout.Space();
            RenderBuild();
        }
    }
}
