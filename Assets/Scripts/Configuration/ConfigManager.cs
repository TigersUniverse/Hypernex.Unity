using System;
using System.IO;
using Tomlet;
using Tomlet.Models;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    private static string persistentAppData;
    
    public static string ConfigLocation => Path.Combine(persistentAppData, "config.cfg");
    public static Config LoadedConfig;

    public static Action<Config> OnConfigSaved = config => { };
    public static Action<Config> OnConfigLoaded = config => { };

    public void Start()
    {
        persistentAppData = Application.persistentDataPath;
        LoadConfigFromFile();
    }

    public void OnApplicationQuit()
    {
        SaveConfigToFile(LoadedConfig);
    }

    public static void LoadConfigFromFile()
    {
        if (File.Exists(ConfigLocation))
        {
            try
            {
                string text = File.ReadAllText(ConfigLocation);
                LoadedConfig = TomletMain.To<Config>(text);
                OnConfigLoaded.Invoke(LoadedConfig);
                Logger.CurrentLogger.Log("Loaded Config");
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
            }
        }
        else
        {
            LoadedConfig = new Config();
            SaveConfigToFile();
        }
    }

    public static void SaveConfigToFile(Config config = null)
    {
        TomlDocument document;
        if (config != null)
            document = TomletMain.DocumentFrom(typeof(Config), config);
        else
            document = TomletMain.DocumentFrom(typeof(Config), LoadedConfig);
        string text = document.SerializedValue;
        File.WriteAllText(ConfigLocation, text);
        OnConfigSaved.Invoke(config);
        Logger.CurrentLogger.Log("Saved Config");
    }
}
