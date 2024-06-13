using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
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
        public static bool EyeTracking => libManager?.LoadedModulesMetadata.Count(x => x.UsingEye && x.Active) > 0;
        public static bool LipTracking => libManager?.LoadedModulesMetadata.Count(x => x.UsingExpression && x.Active) > 0;

        public static List<ICustomFaceExpression> CustomFaceExpressions = new();
        
        private static FaceTrackingServices.FTSettings settings;
        private static FaceTrackingServices.FTLoggerFactory loggerFactory;
        private static FaceTrackingServices.FTDispatcher dispatcher;
        private static ILogger<ModuleDataService> moduleDataServiceLogger;
        private static IModuleDataService moduleDataService;
        private static ILibManager libManager;
        private static ILogger<UnifiedTrackingMutator> mutatorLogger;
        private static UnifiedTrackingMutator mutator;
        private static MainIntegrated mainIntegrated;

        public static async void Init(string persistentData, User user)
        {
            if (HasInitialized)
                return;
            VRCFaceTracking.Core.Utils.PersistentDataDirectory = Path.Combine(persistentData, "VRCFaceTracking");
            VRCFaceTracking.Core.Utils.CustomLibsDirectory = persistentData + "\\VRCFTModules";
            settings = new FaceTrackingServices.FTSettings();
            loggerFactory = new FaceTrackingServices.FTLoggerFactory();
            dispatcher = new FaceTrackingServices.FTDispatcher();
            moduleDataServiceLogger = loggerFactory.CreateLogger<ModuleDataService>();
            mutatorLogger = loggerFactory.CreateLogger<UnifiedTrackingMutator>();
            
            moduleDataService =
                new ModuleDataService(new FaceTrackingServices.HypernexIdentity(user), moduleDataServiceLogger);
            libManager = new UnifiedLibManager(loggerFactory, dispatcher, moduleDataService);
            mutator = new UnifiedTrackingMutator(mutatorLogger, settings);
            mainIntegrated = new MainIntegrated(loggerFactory, libManager, mutator);
            await mainIntegrated.InitializeAsync();
            CustomFaceExpressions.Clear();
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

        public static Dictionary<string, (float, ICustomFaceExpression)> GetFaceWeights()
        {
            Dictionary<string, (float, ICustomFaceExpression)> weights = new();
            if (!HasInitialized || UnifiedTracking.Data == null || UnifiedTracking.Data.Shapes == null)
                return weights;
            int i = 0;
            foreach (UnifiedExpressionShape unifiedExpressionShape in UnifiedTracking.Data.Shapes)
            {
                if (i < (int) FaceExpressions.Max)
                {
                    FaceExpressions faceExpressions = (FaceExpressions) i;
                    weights.Add(faceExpressions.ToString(), (unifiedExpressionShape.Weight, null));
                }
                i++;
            }
            CustomFaceExpressions.ForEach(x =>
            {
                if(weights.ContainsKey(x.Name)) return;
                weights.Add(x.Name, (x.GetWeight(UnifiedTracking.Data), x));
            });
            return weights;
        }

        public static void Destroy()
        {
            if(HasInitialized)
                mainIntegrated.Teardown();
        }
    }
}