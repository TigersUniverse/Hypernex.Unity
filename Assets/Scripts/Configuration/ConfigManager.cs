using System;
using System.IO;
using Tomlet;
using Tomlet.Models;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    public static readonly string ConfigLocation = Path.Combine(Application.persistentDataPath, "config.cfg");
    public static Config LoadedConfig;

    public void OnEnable()
    {
        DontDestroyOnLoad(gameObject);
        LoadConfigFromFile();
    }

    public void OnApplicationQuit()
    {
        SaveConfigToFile(LoadedConfig);
    }

    public void LoadConfigFromFile()
    {
        if (File.Exists(ConfigLocation))
        {
            try
            {
                string text = File.ReadAllText(ConfigLocation);
                LoadedConfig = TomletMain.To<Config>(text);
                Logger.CurrentLogger.Log("Loaded Config");
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
            }
        }
    }

    public void SaveConfigToFile(Config config)
    {
        TomlDocument document = TomletMain.DocumentFrom(typeof(Config), config);
        string text = document.SerializedValue;
        File.WriteAllText(ConfigLocation, text);
        Logger.CurrentLogger.Log("Saved Config");
    }
}
