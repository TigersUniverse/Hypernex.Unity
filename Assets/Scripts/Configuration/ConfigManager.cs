using System;
using System.IO;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.Databasing;
using Tomlet;
using Tomlet.Models;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Configuration
{
    public class ConfigManager : MonoBehaviour
    {
        private static string persistentAppData;
    
        public static string ConfigLocation => Path.Combine(persistentAppData, "config.cfg");
        public static Config LoadedConfig;
        public static ConfigUser SelectedConfigUser;

        public static Action<Config> OnConfigSaved = config => { };
        public static Action<Config> OnConfigLoaded = config => { };

        private static Database userDatabase;

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
                    Logger.CurrentLogger.Debug("Loaded Config");
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Critical(e);
                }
            }
            else
            {
                LoadedConfig = new Config();
                OnConfigLoaded.Invoke(LoadedConfig);
                SaveConfigToFile();
            }
        }

        public static Database GetDatabase()
        {
            if (SelectedConfigUser == null) return null;
            if (userDatabase != null && !userDatabase.IsSame(SelectedConfigUser))
            {
                userDatabase.Dispose();
                userDatabase = null;
            }
            try
            {
                userDatabase ??= new Database(SelectedConfigUser);
                return userDatabase;
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error("Could not load database! " + e);
                return null;
            }
        }

        public static void SaveConfigToFile(Config config = null)
        {
            if (config == null)
                config = LoadedConfig;
            if (config == null)
                return;
            // Clone the SelectedConfigUser
            if (SelectedConfigUser != null)
            {
                ConfigUser docConfigUser = config.GetConfigUserFromUserId(SelectedConfigUser.UserId);
                if(docConfigUser != null)
                    docConfigUser.Clone(SelectedConfigUser);
            }
            TomlDocument document = TomletMain.DocumentFrom(typeof(Config), config);
            string text = document.SerializedValue;
            File.WriteAllText(ConfigLocation, text);
            OnConfigSaved.Invoke(config);
            Logger.CurrentLogger.Debug("Saved Config");
        }
    }
}
