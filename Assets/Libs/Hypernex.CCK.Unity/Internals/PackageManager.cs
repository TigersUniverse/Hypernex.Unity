#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if XRPLUGIN && OPENXR
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
#endif
#if STEAMAUDIO_ENABLED
using SteamAudio;
#endif
#if UI
using UnityEngine.UI;
using TMPro;
#endif
#if MAGICACLOTH2
using MagicaCloth2;
#endif

namespace Hypernex.CCK.Unity.Internals
{
    [InitializeOnLoad]
    public class PackageManager
    {
        private const string XR_PLUGIN_MANAGEMENT_PACKAGE = "com.unity.xr.management";
        private const string OPENXR_PLUGIN_PACKAGE = "com.unity.xr.openxr";

        private const string URP_PACKAGE = "com.unity.render-pipelines.universal";
        private const string UI_PACKAGE = "com.unity.ugui";
        private const string AI_PACKAGE = "com.unity.ai.navigation";
        
        private static ListRequest ListRequest;
        private static List<AddRequest> AddRequests = new ();

        static PackageManager()
        {
            try
            {
                SetRenderingSettings();
                AddScriptingDefineSymbol("HYPERNEX_CCK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Hypernex.CCK.Unity",
                    "Failed to set RenderingSettings! Please make sure you are using URP and that you have a Rendering Asset!",
                    "OK");
                Logger.CurrentLogger.Error(e);
            }
            ListRequest = Client.List();
            EditorApplication.update += ListProgress;
#if XRPLUGIN && OPENXR
            EnableXR();
#endif
        }
        
        // the new scripting define symbols are terrible
#pragma warning disable CS0618 // Type or member is obsolete
        public static void AddScriptingDefineSymbol(string define)
        {
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out defines);
            List<string> clone = new List<string>(defines);
            if(clone.Contains(define)) return;
            clone.Add(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, clone.ToArray());
        }
        
        public static void RemoveScriptingDefineSymbol(string define)
        {
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out defines);
            List<string> clone = new List<string>(defines);
            if(clone.Contains(define))
                clone.Remove(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, clone.ToArray());
        }
#pragma warning restore CS0618 // Type or member is obsolete

        private static void ListProgress()
        {
            if(ListRequest.IsCompleted)
            {
                if(ListRequest.Status == StatusCode.Success)
                {
                    bool containsXRM = false;
                    bool containsOXR = false;
                    bool containsURP = false;
                    bool containsUI = false;
                    bool containsAI = false;
                    foreach (var package in ListRequest.Result)
                    {
                        if(package.name.Contains(XR_PLUGIN_MANAGEMENT_PACKAGE) && package.version == "4.4.0")
                        {
                            containsXRM = true;
                            AddScriptingDefineSymbol("XRPLUGIN");
                        }
                        if(package.name.Contains(OPENXR_PLUGIN_PACKAGE))
                        {
                            containsOXR = true;
                            AddScriptingDefineSymbol("OPENXR");
                        }
                        if (package.name.Contains(URP_PACKAGE))
                        {
                            containsURP = true;
                            AddScriptingDefineSymbol("URP");
                        }
                        if (package.name.Contains(UI_PACKAGE))
                        {
                            containsUI = true;
                            AddScriptingDefineSymbol("UI");
                        }
                        if (package.name.Contains(AI_PACKAGE))
                            containsAI = true;
                    }
                    if (!containsXRM)
                    {
                        AddRequests.Add(Client.Add(XR_PLUGIN_MANAGEMENT_PACKAGE + "@4.4.0"));
                        RemoveScriptingDefineSymbol("XRPLUGIN");
                    }
                    if (!containsOXR)
                    {
                        AddRequests.Add(Client.Add(OPENXR_PLUGIN_PACKAGE));
                        RemoveScriptingDefineSymbol("OPENXR");
                    }
                    if(!containsAI)
                        AddRequests.Add(Client.Add(AI_PACKAGE));
                    if(!containsURP) RemoveScriptingDefineSymbol("URP");
                    if(!containsUI) RemoveScriptingDefineSymbol("UI");
                    EditorApplication.update += AddRequestF;
                    EditorApplication.update -= ListProgress;
                }
            }
        }

        private static void AddRequestF()
        {
            foreach (AddRequest addRequest in new List<AddRequest>(AddRequests))
            {
                if (!addRequest.IsCompleted) continue;
                if (addRequest.Status != StatusCode.Success) continue;
                Debug.Log("Installed " + addRequest.Result.name);
                if(addRequest.Result.name.Contains(XR_PLUGIN_MANAGEMENT_PACKAGE))
                    AddScriptingDefineSymbol("XRPLUGIN");
                if(addRequest.Result.name.Contains(OPENXR_PLUGIN_PACKAGE))
                    AddScriptingDefineSymbol("OPENXR");
                AddRequests.Remove(addRequest);
            }
            if(AddRequests.Count <= 0)
                EditorApplication.update -= AddRequestF;
        }

#if XRPLUGIN && OPENXR
        private static void EnableXR()
        {
            try
            {
                XRGeneralSettings buildTargetSettings =
                    XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(EditorUserBuildSettings
                        .selectedBuildTargetGroup);
                XRManagerSettings xrManagerSettings = buildTargetSettings.AssignedSettings;
                foreach (XRLoader xrLoader in xrManagerSettings.activeLoaders)
                {
                    if (xrLoader.GetType() != typeof(OpenXRLoader))
                        xrManagerSettings.TryRemoveLoader(xrLoader);
                }
                OpenXRSettings.ActiveBuildTargetInstance.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            }
            catch (Exception)
            {
                PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
            }
        }
#endif

        private static void SetRenderingSettings()
        {
            if(UniversalRenderPipeline.asset == null)
                throw new Exception("No RenderPipeline Asset!");
            object a = UniversalRenderPipeline.asset.GetType()
                .GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(UniversalRenderPipeline.asset);
            if (a == null)
                throw new Exception("a is null!");
            ScriptableRendererData[] srd = (ScriptableRendererData[]) a;
            foreach (ScriptableRendererData scriptableRendererData in srd)
            {
                UniversalRendererData urd = (UniversalRendererData) scriptableRendererData;
                urd.depthPrimingMode = DepthPrimingMode.Disabled;
                foreach (ScriptableRendererFeature scriptableRendererFeature in scriptableRendererData.rendererFeatures)
                {
                    if(scriptableRendererFeature == null) continue;
                    bool enable = true;
                    switch (scriptableRendererFeature.name)
                    {
                        case "SSAO":
                            enable = false;
                            break;
                    }
                    scriptableRendererFeature.SetActive(enable);
                }
            }
        }
    }
}
#endif