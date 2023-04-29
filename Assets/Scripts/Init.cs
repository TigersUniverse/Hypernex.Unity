using System.IO;
using Hypernex.Player;
using Hypernex.UI;
using Hypernex.CCK.Unity;
using Hypernex.Tools;
using UnityEngine;

public class Init : MonoBehaviour
{
    public UITheme DefaultTheme;

    private string GetPluginLocation() =>
#if UNITY_EDITOR
        Path.Combine(Application.persistentDataPath, "Plugins");
#else
        Path.Combine(Application.dataPath, "Plugins");
#endif

    private void Start()
    {
        DefaultTheme.ApplyThemeToUI();
        DiscordTools.StartDiscord();
        
        int pluginsLoaded = PluginLoader.LoadAllPlugins(GetPluginLocation());
        Debug.Log($"Loaded {pluginsLoaded} Plugins!");
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
    }
}
