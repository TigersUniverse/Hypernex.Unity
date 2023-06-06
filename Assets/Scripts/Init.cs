using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hypernex.Player;
using Hypernex.UI;
using Hypernex.CCK.Unity;
using Hypernex.Game;
using Hypernex.Tools;
using HypernexSharp.SocketObjects;
using Nexport;
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

    private string worldId;

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
                APIPlayer.UserSocket.RequestNewInstance(result.result.Meta, InstancePublicity.Anyone, InstanceProtocol.KCP);
            }, worldId);
        }
    }

    private void OnApplicationQuit()
    {
        DiscordTools.Stop();
        if (APIPlayer.UserSocket != null && APIPlayer.UserSocket.IsOpen)
            APIPlayer.UserSocket.Close();
        StopVR();
    }
}
