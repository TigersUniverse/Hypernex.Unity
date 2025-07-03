using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFMediaToolkit;
using Hypernex.Player;
using Hypernex.UI;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Configuration;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.ExtendedTracking;
using Hypernex.Game;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using Hypernex.UI.Components;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
#if VLC
using Hypernex.Game.Video;
using LibVLCSharp;
#endif
using Logger = Hypernex.CCK.Logger;
using Material = UnityEngine.Material;
using Object = UnityEngine.Object;

public class Init : MonoBehaviour
{
    internal const string DEFAULT_DOMAIN = "play.hypernex.dev";
    internal const string DEFAULT_WEB_URL = "https://" + DEFAULT_DOMAIN;
    internal const string WEBSITE = "https://hypernex.dev/";
    internal const string FORUM_URL = "https://forum.hypernex.dev/";
    internal const string GITHUB_URL = "https://github.com/TigersUniverse";
    internal const string DISCORD_URL = WEBSITE + "discord";
    internal const string X_URL = "https://x.com/HypernexGame";
    internal const string BLUESKY_URL = "https://bsky.app/profile/hypernex.dev";
    
    public string Version => Application.version;

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
    public AudioMixerGroup AvatarGroup;
    public OverlayNotification OverlayManager;
    public List<TMP_Text> VersionLabels = new();
    public Texture2D MouseTexture;
    public Texture2D CircleMouseTexture;
    public CreateInstanceWindow CreateInstanceWindow;
    public float SmoothingFrames = 0.1f;
    public List<Object> BadgeRankAssets = new();
    public bool NoVLC;
    public VolumeProfile DefaultVolumeProfile;

    internal Dictionary<AudioMixerGroup, AudioMixer> audioMixers = new();

    public string GetPluginLocation() => Path.Combine(Application.persistentDataPath, "Plugins");
    public string GetDatabaseLocation() => Path.Combine(Application.persistentDataPath, "Databases");
    public string GetYTDLLocation() => Path.Combine(Application.streamingAssetsPath, "ytdl");

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
        CursorTools.UpdateMouseIcon(true, DefaultTheme.PrimaryColorTheme);
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
        /*try
        {
            StartVR();
            SystemHeadset systemHeadset = Utils.GetSystemHeadsetType();
            bool isOculus = systemHeadset != SystemHeadset.None;
            if (!isOculus)
                StopVR();
        } catch(Exception e){Logger.CurrentLogger.Critical(e);}*/
#if !UNITY_EDITOR
        StartVR();
#endif
#endif
        string[] args = Environment.GetCommandLineArgs();
        DownloadTools.forceHttpClient = args.Contains("--force-http-downloads");
        NoVLC = args.Contains("--no-vlc");
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
        SecurityTools.AllowExtraTypes();
        ExtraSandboxTools.ImplementRestrictions();
        kTools.Mirrors.Mirror.OnMirrorCreation += mirror => mirror.CustomCameraControl = true;
        RenderPipelineManager.beginCameraRendering += BeginRender_NoAvatar;
        AvatarNearClip.BeforeClip += BeginRender_Avatar;
        DefaultTheme.ApplyThemeToUI();
        VersionLabels.ForEach(x => x.text = Version);
        DiscordTools.StartDiscord();
        GeoTools.Init();
        TimeSettings.Is24HourClock = () => DateTools.Is24H;
        audioMixers.Add(VoiceGroup, VoiceGroup.audioMixer);
        audioMixers.Add(WorldGroup, WorldGroup.audioMixer);
        audioMixers.Add(AvatarGroup, AvatarGroup.audioMixer);
        if (!Directory.Exists(GetYTDLLocation()))
            Directory.CreateDirectory(GetYTDLLocation());
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        Streaming.ytdl.YoutubeDLPath = Path.Combine(GetYTDLLocation(), "yt-dlp.exe");
        Streaming.ytdl.FFmpegPath = Path.Combine(GetYTDLLocation(), "ffmpeg.exe");
#elif UNITY_MAC
        Streaming.ytdl.YoutubeDLPath = Path.Combine(GetYTDLLocation(), "yt-dlp_macos");
        Streaming.ytdl.FFmpegPath = Path.Combine(GetYTDLLocation(), "ffmpeg");
#else
        Streaming.ytdl.YoutubeDLPath = Path.Combine(GetYTDLLocation(), "yt-dlp");
        Streaming.ytdl.FFmpegPath = Path.Combine(GetYTDLLocation(), "ffmpeg");
#endif
        Streaming.ytdl.OutputFolder = Path.Combine(GetYTDLLocation(), "Downloads");
        YoutubeDLSharp.Utils.DownloadBinaries(true, GetYTDLLocation());
        string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "ffmpeg");
        FFMpegDownloader.Download(ffmpegPath);
        FFmpegLoader.FFmpegPath = ffmpegPath;
        FFmpegLoader.LoadFFmpeg();

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
                if (configUser.UseFacialTracking)
                    FaceTrackingManager.Init(targetStreamingPath, user);
            }
            WebHandler.HandleLaunchArgs(args, CreateInstanceWindow);
        };
        GetComponent<CoroutineRunner>()
            .Run(LocalPlayer.SafeSwitchScene(1, null,
                s =>
                {
                    LocalPlayer.transform.position =
                        s.GetRootGameObjects().First(x => x.name == "Spawn").transform.position;
                    LocalPlayer.Dashboard.PositionDashboard(LocalPlayer);
                }));
    }

    private void BeginRender_Avatar(ScriptableRenderContext context, Camera c) =>
        kTools.Mirrors.Mirror.Mirrors.ForEach(x => x.OnCameraRender.Invoke(context, c));

    private void BeginRender_NoAvatar(ScriptableRenderContext context, Camera c)
    {
        if(AvatarNearClip.Instances.Count > 0 && !AvatarNearClip.UseFallback()) return;
        kTools.Mirrors.Mirror.Mirrors.ForEach(x => x.OnCameraRender.Invoke(context, c));
    }

    private void FixedUpdate() => GameInstance.FocusedInstance?.FixedUpdate();

    private void Update()
    {
        DiscordTools.RunCallbacks();
        if (ConfigManager.SelectedConfigUser != null)
        {
            audioMixers[VoiceGroup].SetFloat("volume", ConfigManager.SelectedConfigUser.VoicesBoost);
            audioMixers[WorldGroup].SetFloat("volume", ConfigManager.SelectedConfigUser.WorldAudioVolume);
        }
        GameInstance.FocusedInstance?.Update();
        DownloadTools.Check();
    }
    
    private void LateUpdate() => GameInstance.FocusedInstance?.LateUpdate();

    private void OnApplicationQuit()
    {
        RenderPipelineManager.beginCameraRendering -= BeginRender_NoAvatar;
        AvatarNearClip.BeforeClip -= BeginRender_Avatar;
        foreach (KeyValuePair<string, string> avatarIdToken in LocalPlayer.OwnedAvatarIdTokens)
            APIPlayer.APIObject.RemoveAssetToken(_ => { }, APIPlayer.APIUser, APIPlayer.CurrentToken, avatarIdToken.Key,
                avatarIdToken.Value);
        if(LocalPlayer != null)
            LocalPlayer.Dispose();
        if(GameInstance.FocusedInstance != null)
            GameInstance.FocusedInstance.Dispose();
        if (APIPlayer.UserSocket != null && APIPlayer.UserSocket.IsOpen)
            APIPlayer.UserSocket.Close();
        ConfigManager.GetDatabase()?.Dispose();
        OverlayManager.Dispose();
        DiscordTools.Stop();
        StopVR();
        AssetBundleTools.UnloadAllAssetBundles();
    }
}
