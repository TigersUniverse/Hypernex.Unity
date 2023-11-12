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
using Hypernex.Tools;
using Hypernex.UI.Templates;
using Hypernex.UIActions;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
#if UNITY_ANDROID
using System.Runtime.InteropServices;
using Unity.XR.Oculus;
using UnityEngine.Android;
#endif
using UnityEngine.Audio;
using UnityEngine.XR.Management;
using Logger = Hypernex.CCK.Logger;
using Material = UnityEngine.Material;

public class Init : MonoBehaviour
{
    public const string VERSION = "2023.11.1b";

    public static Init Instance;
    public static bool IsQuitting { get; private set; }

    public LocalPlayer LocalPlayer;
    public UITheme DefaultTheme;
    public bool UseHTTP;
    public RuntimeAnimatorController DefaultAvatarAnimatorController;
    public Material OutlineMaterial;
    public List<TMP_SpriteAsset> EmojiSprites = new ();
    public AudioMixerGroup VoiceGroup;
    public AudioMixerGroup WorldGroup;
    public OverlayManager OverlayManager;
    public List<TMP_Text> VersionLabels = new();
    public CurrentAvatar ca;
    public Texture2D MouseTexture;
    public Texture2D CircleMouseTexture;

    public string GetPluginLocation() => Path.Combine(Application.persistentDataPath, "Plugins");

    internal void StartVR()
    {
        if (LocalPlayer.IsVR) return;
        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
        LocalPlayer.IsVR = true;
        LocalPlayer.StartVR();
    }

    internal void StopVR()
    {
        if (!LocalPlayer.IsVR) return;
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        LocalPlayer.IsVR = false;
        LocalPlayer.StopVR();
    }

    private void Start()
    {
        Instance = this;
        UnityLogger unityLogger = new UnityLogger();
        unityLogger.SetLogger();
        CursorTools.UpdateMouseIcon(true, DefaultTheme.PrimaryVectorColor);
        OverlayManager.Begin();
        Application.wantsToQuit += () =>
        {
            FaceTrackingManager.Destroy();
            IsQuitting = true;
            return true;
        };
        kcp2k.Log.Info = s => unityLogger.Debug(s);
        kcp2k.Log.Warning = s => unityLogger.Warn(s);
        kcp2k.Log.Error = s => unityLogger.Error(s);
        Telepathy.Log.Info = s => unityLogger.Debug(s);
        Telepathy.Log.Warning = s => unityLogger.Warn(s);
        Telepathy.Log.Error = s => unityLogger.Error(s);
        Application.backgroundLoadingPriority = ThreadPriority.Low;
#if UNITY_ANDROID
        //Caching.compressionEnabled = false;
        try
        {
            if(!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
            if(!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
        catch(Exception){}
        try
        {
            StartVR();
            SystemHeadset systemHeadset = Utils.GetSystemHeadsetType();
            bool isOculus = systemHeadset != SystemHeadset.None;
            if (!isOculus)
                StopVR();
        } catch(Exception e){Logger.CurrentLogger.Critical(e);}
#endif
        string[] args = Environment.GetCommandLineArgs();
        DownloadTools.forceHttpClient = args.Contains("--force-http-downloads");
        if(args.Contains("-xr") && !LocalPlayer.IsVR)
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
        VersionLabels.ForEach(x => x.text = VERSION);
        DiscordTools.StartDiscord();
        GeoTools.Init();

        int pluginsLoaded;
        try
        {
            pluginsLoaded = PluginLoader.LoadAllPlugins(GetPluginLocation());
        }
        catch (Exception)
        {
            pluginsLoaded = 0;
        }
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
        CurrentAvatar.Instance = ca;
        GetComponent<CoroutineRunner>()
            .Run(LocalPlayer.SafeSwitchScene(1, null,
                s =>
                {
                    LocalPlayer.transform.position =
                        s.GetRootGameObjects().First(x => x.name == "Spawn").transform.position;
                    LocalPlayer.Dashboard.PositionDashboard(LocalPlayer);
                }));
    }

    private void FixedUpdate() => GameInstance.FocusedInstance?.FixedUpdate();

    private void Update()
    {
        DiscordTools.RunCallbacks();
        if (ConfigManager.SelectedConfigUser != null)
        {
            VoiceGroup.audioMixer.SetFloat("volume", ConfigManager.SelectedConfigUser.VoicesBoost);
            WorldGroup.audioMixer.SetFloat("volume", ConfigManager.SelectedConfigUser.WorldAudioVolume);
        }
        GameInstance.FocusedInstance?.Update();
    }
    
    private void LateUpdate() => GameInstance.FocusedInstance?.LateUpdate();

    private void OnApplicationQuit()
    {
        foreach (KeyValuePair<string, string> avatarIdToken in LocalPlayer.OwnedAvatarIdTokens)
            APIPlayer.APIObject.RemoveAssetToken(_ => { }, APIPlayer.APIUser, APIPlayer.CurrentToken, avatarIdToken.Key,
                avatarIdToken.Value);
        if(LocalPlayer != null)
            LocalPlayer.Dispose();
        if(GameInstance.FocusedInstance != null)
            GameInstance.FocusedInstance.Dispose();
        if (APIPlayer.UserSocket != null && APIPlayer.UserSocket.IsOpen)
            APIPlayer.UserSocket.Close();
        OverlayManager.Dispose();
        DiscordTools.Stop();
        StopVR();
        AssetBundleTools.UnloadAllAssetBundles();
    }
}
