using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.Tools;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Models;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Services;

namespace Hypernex.ExtendedTracking
{
    public static class FaceTrackingManager
    {
        public static bool HasInitialized { get; private set; }
        
        private static FaceTrackingServices.FTSettings settings;
        private static FaceTrackingServices.FTLoggerFactory loggerFactory;
        private static FaceTrackingServices.FTDispatcher dispatcher;
        private static ILogger<ModuleDataService> moduleDataServiceLogger;
        private static IModuleDataService moduleDataService;
        private static ILibManager libManager;
        private static ILogger<UnifiedTrackingMutator> mutatorLogger;
        private static UnifiedTrackingMutator mutator;
        private static MainIntegrated mainIntegrated;

        public static void Init(string persistentData)
        {
            if (HasInitialized)
                return;
            Utils.PersistentDataDirectory = Path.Combine(persistentData, "VRCFaceTracking");
            Utils.CustomLibsDirectory = persistentData + "\\CustomLibs";
            settings = new FaceTrackingServices.FTSettings();
            loggerFactory = new FaceTrackingServices.FTLoggerFactory();
            dispatcher = new FaceTrackingServices.FTDispatcher();
            moduleDataServiceLogger = loggerFactory.CreateLogger<ModuleDataService>();
            mutatorLogger = loggerFactory.CreateLogger<UnifiedTrackingMutator>();

            moduleDataService = new ModuleDataService(moduleDataServiceLogger);
            libManager = new UnifiedLibManager(loggerFactory, dispatcher, moduleDataService);
            mutator = new UnifiedTrackingMutator(mutatorLogger, dispatcher, settings);
            mainIntegrated = new MainIntegrated(loggerFactory, libManager, mutator);
            mainIntegrated.InitializeAsync();
            HasInitialized = true;
        }

        public static List<InstallableTrackingModule> GetDownloadableDependencies() => !HasInitialized
            ? Array.Empty<InstallableTrackingModule>().ToList()
            : moduleDataService.GetRemoteModules().Result.ToList();

        public static void InstallDependency(InstallableTrackingModule trackingModule, Action callback = null) =>
            DownloadTools.DownloadFile(trackingModule.DownloadUrl, trackingModule.DllFileName, s =>
            {
                File.Move(s, Path.Combine(Utils.CustomLibsDirectory, s));
                callback?.Invoke();
            });

        public static T GetSettings<T>(string key) => settings.ReadSettingAsync<T>(key).Result;
        public static void SetSettings<T>(string key, T value) => settings.SaveSettingAsync(key, value);

        public static UnifiedEyeData GetEyeWeights() => !HasInitialized ? null : UnifiedTracking.Data.Eye;

        public static Dictionary<FaceExpressions, float> GetFaceWeights()
        {
            Dictionary<FaceExpressions, float> weights = new();
            if (!HasInitialized || UnifiedTracking.Data == null || UnifiedTracking.Data.Shapes == null)
                return weights;
            int i = 0;
            foreach (UnifiedExpressionShape unifiedExpressionShape in UnifiedTracking.Data.Shapes)
            {
                if (i < (int) FaceExpressions.Max)
                {
                    FaceExpressions faceExpressions = (FaceExpressions) i;
                    weights.Add(faceExpressions, unifiedExpressionShape.Weight);
                }
                i++;
            }
            return weights;
        }

        public static void Destroy()
        {
            if(HasInitialized)
                mainIntegrated.Teardown();
        }
    }
}