using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.CCK.Unity.Interaction;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using Microsoft.Extensions.Logging;
using UnityEngine;
using UnityEngine.UI;
using VRCFaceTracking;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Models;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Services;
using Image = VRCFaceTracking.Core.Types.Image;
using Logger = Hypernex.CCK.Logger;
using Utils = VRCFaceTracking.Core.Utils;

namespace Hypernex.ExtendedTracking
{
    public static class FaceTrackingManager
    {
        public static bool HasInitialized { get; private set; }
        public static bool EyeTracking => libManager?.LoadedModulesMetadata.Count(x => x.UsingEye && x.Active) > 0;
        public static bool LipTracking => libManager?.LoadedModulesMetadata.Count(x => x.UsingExpression && x.Active) > 0;
        public static Action<UnifiedTrackingData> OnTrackingUpdated = data => { };

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
            Utils.PersistentDataDirectory = Path.Combine(persistentData, "VRCFaceTracking");
            if (!Directory.Exists(Utils.PersistentDataDirectory))
                Directory.CreateDirectory(Utils.PersistentDataDirectory);
            Utils.CustomLibsDirectory = persistentData + "\\VRCFTModules";
            if (!Directory.Exists(Utils.CustomLibsDirectory))
                Directory.CreateDirectory(Utils.CustomLibsDirectory);
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
            UnifiedTracking.OnUnifiedDataUpdated +=
                data => QuickInvoke.InvokeImmediate(() => OnTrackingUpdated.Invoke(data));
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
        
        private static void CreateTextureFromImage(Image image, RawImage rawImage)
        {
            try
            {
                Texture2D texture2D;
                try
                {
                    texture2D = (Texture2D) rawImage.texture;
                }
                catch (Exception)
                {
                    texture2D = null;
                }
                if(texture2D == null)
                {
                    texture2D ??= new Texture2D(image.ImageSize.x, image.ImageSize.y, TextureFormat.RGBA32, false);
                    rawImage.texture = texture2D;
                }
                texture2D.LoadRawTextureData(image.ImageData);
                texture2D.Apply(false);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error("Failed to get image texture! " + e);
            }
        }

        public static void SetCameraTextures(ref RawImage eyes, ref RawImage lips)
        {
            if (!HasInitialized) return;
            if(UnifiedTracking.EyeImageData.SupportsImage)
                CreateTextureFromImage(UnifiedTracking.EyeImageData, eyes);
            if (UnifiedTracking.LipImageData.SupportsImage)
                CreateTextureFromImage(UnifiedTracking.LipImageData, lips);
        }
        
        public static void Restart()
        {
            if(!HasInitialized || mainIntegrated == null) return;
            HasInitialized = false;
            mainIntegrated.Teardown();
            mainIntegrated.InitializeAsync();
            HasInitialized = true;
        }

        public static void Destroy()
        {
            if(HasInitialized)
            {
                mainIntegrated?.Teardown();
                HasInitialized = false;
            }
        }
    }
}