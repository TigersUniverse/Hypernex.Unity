using System;
using System.IO;
using System.Linq;
using Hypernex.Player;
using Hypernex.UI;
using Hypernex.CCK.Unity;
using Hypernex.Tools;
using UnityEngine;
using UnityEngine.XR.Management;
using Logger = Hypernex.CCK.Logger;

public class Init : MonoBehaviour
{
    public UITheme DefaultTheme;
    public bool ForceVR;

    private string GetPluginLocation() =>
#if UNITY_EDITOR
        Path.Combine(Application.persistentDataPath, "Plugins");
#else
        Path.Combine(Application.dataPath, "Plugins");
#endif

    private void StartVR()
    {
        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
    }

    private void StopVR()
    {
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
    }

    private void Start()
    {
        UnityLogger unityLogger = new UnityLogger();
        unityLogger.SetLogger();
        string[] args = Environment.GetCommandLineArgs();
        if(ForceVR || args.Contains("-xr"))
            StartVR();
        DownloadTools.DownloadsPath = Path.Combine(Application.streamingAssetsPath, "Downloads");
        DefaultTheme.ApplyThemeToUI();
        DiscordTools.StartDiscord();
        
        int pluginsLoaded = PluginLoader.LoadAllPlugins(GetPluginLocation());
        Logger.CurrentLogger.Log($"Loaded {pluginsLoaded} Plugins!");
        gameObject.AddComponent<PluginLoader>();
    }

    private void Update()
    {
        DiscordTools.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        DiscordTools.Stop();
        if (APIPlayer.UserSocket != null && APIPlayer.UserSocket.IsOpen)
            APIPlayer.UserSocket.Close();
        StopVR();
    }
}
