using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Hypernex.CCK.Unity.Internals
{
    public class PluginLoader : MonoBehaviour
    {
        private static readonly List<HypernexPlugin> _loadedPlugins = new List<HypernexPlugin>();
        public static List<HypernexPlugin> LoadedPlugins => new List<HypernexPlugin>(_loadedPlugins);

        private static readonly string[] Libs = {"Hypernex.CCK.Unity.Libs.Lib.Harmony.0Harmony.dll"};

        public static int LoadAllPlugins(string path)
        {
            int pluginsLoaded = 0;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return 0;
            }
            foreach (string possiblePluginFile in Directory.GetFiles(path))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(Path.GetFullPath(possiblePluginFile));
                    List<Type> plugins = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(HypernexPlugin)))
                        .ToList();
                    foreach (Type pluginType in plugins)
                    {
                        HypernexPlugin hypernexPlugin = (HypernexPlugin) Activator.CreateInstance(pluginType);
                        if (hypernexPlugin == null)
                            throw new Exception("Failed to create instance from HypernexPlugin!");
                        typeof(HypernexPlugin).GetProperty("Logger")?.SetValue(hypernexPlugin, new UnityPluginLogger(hypernexPlugin.PluginName));
                        Logger.CurrentLogger.Log($"Loaded Plugin {hypernexPlugin.PluginName} by {hypernexPlugin.PluginCreator} ({hypernexPlugin.PluginVersion})");
                        _loadedPlugins.Add(hypernexPlugin);
                        pluginsLoaded++;
                        hypernexPlugin.OnPluginLoaded();
                    }
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Error("Failed to Load Plugin at " + possiblePluginFile + " for reason " + e);
                }
            }
            return pluginsLoaded;
        }

        private void Start()
        {
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
                hypernexPlugin.Start();
        }

        private void FixedUpdate()
        {
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
                hypernexPlugin.FixedUpdate();
        }

        private void Update()
        {
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
                hypernexPlugin.Update();
        }

        private void LateUpdate()
        {
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
                hypernexPlugin.LateUpdate();
        }

        private void OnApplicationQuit()
        {
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
            {
                hypernexPlugin.OnApplicationExit();
                _loadedPlugins.Remove(hypernexPlugin);
            }
        }
    }
}