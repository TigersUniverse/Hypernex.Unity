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
using Hypernex.UIActions;
using HypernexSharp.APIObjects;
using Nexbox;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Audio;
using UnityEngine.XR.Management;
using Logger = Hypernex.CCK.Logger;
using Material = UnityEngine.Material;

public class Init : MonoBehaviour
{
    public static Init Instance;
    public static bool IsQuitting { get; private set; }
    
    public UITheme DefaultTheme;
    public bool UseHTTP;
    public RuntimeAnimatorController DefaultAvatarAnimatorController;
    public Material OutlineMaterial;
    public List<TMP_SpriteAsset> EmojiSprites = new ();
    public AudioMixerGroup VoiceGroup;
    public OverlayManager OverlayManager;

    private string GetPluginLocation() =>
#if UNITY_EDITOR
        Path.Combine(Application.persistentDataPath, "Plugins");
#elif UNITY_ANDROID
        Path.Combine(Application.persistentDataPath, "Plugins");
#else
        Path.Combine(Application.dataPath, "Plugins");
#endif

    internal void StartVR()
    {
        if (LocalPlayer.IsVR) return;
        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
        LocalPlayer.IsVR = true;
        LocalPlayer.Instance.StartVR();
    }

    internal void StopVR()
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
        OverlayManager.Begin();
        Application.wantsToQuit += () => IsQuitting = true;
        kcp2k.Log.Info = s => unityLogger.Debug(s);
        kcp2k.Log.Warning = s => unityLogger.Warn(s);
        kcp2k.Log.Error = s => unityLogger.Error(s);
        Telepathy.Log.Info = s => unityLogger.Debug(s);
        Telepathy.Log.Warning = s => unityLogger.Warn(s);
        Telepathy.Log.Error = s => unityLogger.Error(s);
#if UNITY_ANDROID
        Caching.compressionEnabled = false;
        Application.backgroundLoadingPriority = ThreadPriority.High;
        try
        {
            if(!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
            if(!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
        catch(Exception){}
#else
        Application.backgroundLoadingPriority = ThreadPriority.Low;
#endif
        string[] args = Environment.GetCommandLineArgs();
        if(args.Contains("-xr") || AssetBundleTools.Platform == BuildPlatform.Android)
            StartVR();
        string targetStreamingPath;
        switch (AssetBundleTools.Platform)
        {
            case BuildPlatform.Android:
                DownloadTools.DownloadsPath = Path.Combine(Application.persistentDataPath, "Downloads");
                targetStreamingPath = Application.persistentDataPath;
                break;
            default:
                DownloadTools.DownloadsPath = Path.Combine(Application.streamingAssetsPath, "Downloads");
                targetStreamingPath = Application.streamingAssetsPath;
                break;
        }
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
            if (configUser != null)
            {
                UITheme userTheme = UITheme.GetUIThemeByName(configUser.Theme);
                if(userTheme != null)
                    userTheme.ApplyThemeToUI();
                if(configUser.UseFacialTracking)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        FaceTrackingManager.Init(targetStreamingPath)));
            }
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
        if (ConfigManager.SelectedConfigUser != null)
            VoiceGroup.audioMixer.SetFloat("volume", ConfigManager.SelectedConfigUser.VoicesBoost);
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

    private void OnApplicationQuit()
    {
        foreach (KeyValuePair<string, string> avatarIdToken in LocalPlayer.Instance.OwnedAvatarIdTokens)
            APIPlayer.APIObject.RemoveAssetToken(_ => { }, APIPlayer.APIUser, APIPlayer.CurrentToken, avatarIdToken.Key,
                avatarIdToken.Value);
        if(LocalPlayer.Instance != null)
            LocalPlayer.Instance.Dispose();
        if(GameInstance.FocusedInstance != null)
            GameInstance.FocusedInstance.Dispose();
        if (APIPlayer.UserSocket != null && APIPlayer.UserSocket.IsOpen)
            APIPlayer.UserSocket.Close();
        OverlayManager.Dispose();
        DiscordTools.Stop();
        FaceTrackingManager.Destroy();
        StopVR();
        AssetBundleTools.UnloadAllAssetBundles();
    }
}
