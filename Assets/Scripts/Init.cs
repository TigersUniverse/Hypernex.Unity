using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.Player;
using Hypernex.UI;
using Hypernex.CCK.Unity;
using Hypernex.Configuration;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.ExtendedTracking;
using Hypernex.Game;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using HypernexSharp.SocketObjects;
using Nexbox;
using UnityEngine;
using UnityEngine.XR.Management;
using Logger = Hypernex.CCK.Logger;
using Material = UnityEngine.Material;

public class Init : MonoBehaviour
{
    public static Init Instance;
    
    public UITheme DefaultTheme;
    public bool ForceVR;
    public RuntimeAnimatorController DefaultAvatarAnimatorController;
    public InstanceProtocol InstanceProtocol = InstanceProtocol.KCP;
    public Material OutlineMaterial;

    private string GetPluginLocation() =>
#if UNITY_EDITOR
        Path.Combine(Application.persistentDataPath, "Plugins");
#else
        Path.Combine(Application.dataPath, "Plugins");
#endif

    private void StartVR()
    {
        if (LocalPlayer.IsVR) return;
        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
        LocalPlayer.IsVR = true;
        LocalPlayer.Instance.StartVR();
    }

    private void StopVR()
    {
        if (!LocalPlayer.IsVR) return;
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        LocalPlayer.IsVR = false;
        LocalPlayer.Instance.StopVR();
    }

    private void Start()
    {
        Instance = this;
        UnityLogger unityLogger = new UnityLogger();
        unityLogger.SetLogger();
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        string[] args = Environment.GetCommandLineArgs();
        if(ForceVR || args.Contains("-xr"))
            StartVR();
        DownloadTools.DownloadsPath = Path.Combine(Application.streamingAssetsPath, "Downloads");
        DefaultTheme.ApplyThemeToUI();
        DiscordTools.StartDiscord();
        
        int pluginsLoaded = PluginLoader.LoadAllPlugins(GetPluginLocation());
        Logger.CurrentLogger.Log($"Loaded {pluginsLoaded} Plugins!");
        gameObject.AddComponent<PluginLoader>();
        APIPlayer.OnUser += user =>
        {
            if (ConfigManager.LoadedConfig == null)
                return;
            ConfigUser configUser = ConfigManager.SelectedConfigUser;
            if (configUser == null)
                ConfigManager.LoadedConfig.GetConfigUserFromUserId(user.Id);
            if (configUser != null && configUser.UseFacialTracking)
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    FaceTrackingManager.Init(Application.streamingAssetsPath)));
        };
        GetComponent<CoroutineRunner>()
            .Run(LocalPlayer.Instance.SafeSwitchScene(1, null,
                s =>
                {
                    LocalPlayer.Instance.transform.position =
                        s.GetRootGameObjects().First(x => x.name == "Spawn").transform.position;
                    LocalPlayer.Instance.Dashboard.PositionDashboard(LocalPlayer.Instance);
                }));
    }

    private void Update()
    {
        DiscordTools.RunCallbacks();
        foreach (SandboxFunc sandboxAction in Runtime.OnUpdates)
            try
            {
                SandboxFuncTools.InvokeSandboxFunc(sandboxAction);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
            }
    }

    private void LateUpdate()
    {
        foreach (SandboxFunc sandboxAction in Runtime.OnLateUpdates)
            try
            {
                SandboxFuncTools.InvokeSandboxFunc(sandboxAction);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
            }
    }

    /*private string worldId;

    private void OnGUI()
    {
        worldId = GUILayout.TextField(worldId);
        if (GUILayout.Button("Create Instance From WorldId"))
        {
            if (!APIPlayer.IsFullReady)
                return;
            APIPlayer.APIObject.GetWorldMeta(result =>
            {
                if (!result.success)
                    return;
                APIPlayer.UserSocket.RequestNewInstance(result.result.Meta, InstancePublicity.Anyone, InstanceProtocol.UDP);
            }, worldId);
        }
        if (FaceTrackingManager.HasInitialized)
        {
            GUILayout.Label("Left Eye", new GUIStyle{fontStyle = FontStyle.Bold});
            GUILayout.Label("Gaze: (" + UnifiedTracking.Data.Eye.Left.Gaze.x + "," + UnifiedTracking.Data.Eye.Left.Gaze.y + ")");
            GUILayout.Label("Pupil Diameter: " + UnifiedTracking.Data.Eye.Left.PupilDiameter_MM);
            GUILayout.Label("Openness: " + UnifiedTracking.Data.Eye.Left.Openness);
            GUILayout.Label("Right Eye", new GUIStyle{fontStyle = FontStyle.Bold});
            GUILayout.Label("Gaze: (" + UnifiedTracking.Data.Eye.Right.Gaze.x + "," + UnifiedTracking.Data.Eye.Right.Gaze.y + ")");
            GUILayout.Label("Pupil Diameter: " + UnifiedTracking.Data.Eye.Right.PupilDiameter_MM);
            GUILayout.Label("Openness: " + UnifiedTracking.Data.Eye.Right.Openness);
            GUILayout.Label("Face", new GUIStyle{fontStyle = FontStyle.Bold});
            int i = 0;
            foreach (UnifiedExpressionShape unifiedExpressionShape in UnifiedTracking.Data.Shapes)
            {
                try
                {
                    UnifiedExpressions unifiedExpressions = (UnifiedExpressions) i;
                    GUILayout.Label(unifiedExpressions + ": " + unifiedExpressionShape.Weight);
                }
                catch(Exception){}
                i++;
            }
        }
    }*/

    private void OnApplicationQuit()
    {
        foreach (KeyValuePair<string, string> avatarIdToken in LocalPlayer.Instance.OwnedAvatarIdTokens)
            APIPlayer.APIObject.RemoveAssetToken(_ => { }, APIPlayer.APIUser, APIPlayer.CurrentToken, avatarIdToken.Key,
                avatarIdToken.Value);
        if(LocalPlayer.Instance != null)
            LocalPlayer.Instance.Dispose();
        FaceTrackingManager.Destroy();
        if(GameInstance.FocusedInstance != null)
            GameInstance.FocusedInstance.Dispose();
        DiscordTools.Stop();
        if (APIPlayer.UserSocket != null && APIPlayer.UserSocket.IsOpen)
            APIPlayer.UserSocket.Close();
        AssetBundleTools.UnloadAllAssetBundles();
        StopVR();
    }
}
