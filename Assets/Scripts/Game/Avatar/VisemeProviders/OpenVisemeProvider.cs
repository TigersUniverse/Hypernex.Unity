using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.Tools;
using OpenLipSync.Inference;
using OpenLipSync.Inference.OVRCompat;
using Unity.Mathematics;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Game.Avatar.VisemeProviders
{
    public class OpenVisemeProvider : IVisemeProvider
    {
        private const int SAMPLE_RATE = 48000;

        private static string ModelDirectory => Path.Combine(AssetBundleTools.StreamingLocation, "model");

        internal static void DownloadModel()
        {
            if(Directory.Exists(ModelDirectory)) return;
            const string url = "https://github.com/KyuubiYoru/OpenLipSync/releases/download/0.2.0/model.zip";
            string outputFile = Path.Combine(AssetBundleTools.StreamingLocation, "model.zip");
            DownloadTools.DownloadFile(url, outputFile, file =>
            {
                ZipFile.ExtractToDirectory(file, AssetBundleTools.StreamingLocation);
                File.Delete(file);
                Logger.CurrentLogger.Log("Downloaded OpenLipSync Model!");
            });
        }
        
        private static int ComputeBufferSizeFrom48kWindow(int sampleRate)
        {
            return (int)math.round(sampleRate * (double)1024 / 48000.0);
        }

        public bool Enabled { get; set; } = true;
        public bool IsInitialized => _ovrLipSync.IsInitialized && _analysisContext?.IsInitialized == true;
        public float Smoothing { get; set; } = 0.7f;

        private OpenLipSyncBackend backend;
        private int bufferSize;
        private readonly IOvrLipSyncBackend _backend;
        private readonly OVRLipSyncInterface _ovrLipSync;
        private OpenLipSync.Inference.OVRCompat.OVRLipSyncContext? _analysisContext;
        private float[]? _buffer;
        private readonly float[] _analysis = new float[16];
        private Dictionary<string, float> allVisemes = new Dictionary<string, float>();
        private AvatarCreator ac;
        private BlendshapeDescriptor[] b;

        public void SetupLocal(AvatarCreator avatarCreator, BlendshapeDescriptor[] blendshapes)
        {
            ac = avatarCreator;
            b = blendshapes;
        }
        
        public void SetupNet(AvatarCreator avatarCreator, BlendshapeDescriptor[] blendshapes)
        {
            ac = avatarCreator;
            b = blendshapes;
        }

        public OpenVisemeProvider()
        {
            backend = new OpenLipSyncBackend();
            bufferSize = ComputeBufferSizeFrom48kWindow(SAMPLE_RATE);
            _ovrLipSync = new OVRLipSyncInterface(backend, SAMPLE_RATE, bufferSize, ModelDirectory);
            if (!_ovrLipSync.IsInitialized)
            {
                Result r = (Result) typeof(OVRLipSyncInterface).GetField("_initResult",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_ovrLipSync);
                Logger.CurrentLogger.Error(r + " : " + backend.LastError);
                throw new Exception("Could not initialize OpenLipSync Backend");
            }
            Array.Clear(_analysis, 0, _analysis.Length);
            _analysisContext = new OpenLipSync.Inference.OVRCompat.OVRLipSyncContext(_ovrLipSync);
            if (!_analysisContext.IsInitialized)
            {
                throw new Exception("Could not initialize OpenLipSync Analysis Context");
            }
            Logger.CurrentLogger.Debug("Started OpenVisemeProvider!");
        }

        void IVisemeProvider.ApplyLocal(float[] data) => Apply(data);
        void IVisemeProvider.ApplyNet(float[] data) => Apply(data);

        private void Apply(float[] data)
        {
            if (!Enabled)
            {
                Array.Clear(_analysis, 0, _analysis.Length);
                return;
            }
            for (int offset = 0; offset < data.Length; offset += bufferSize)
            {
                int remaining = data.Length - offset;
                int count = Math.Min(bufferSize, remaining);
                ReadOnlySpan<float> audioData = data.AsSpan(offset, count);
                if (audioData.Length > 0)
                {
                    if (_buffer == null || _buffer.Length != bufferSize)
                    {
                        _buffer = new float[bufferSize];
                    }
                    Array.Clear(_buffer, 0, _buffer.Length);
                    int copyCount = Math.Min(audioData.Length, bufferSize);
                    audioData.Slice(0, copyCount).CopyTo(_buffer);
                    if (_analysisContext != null)
                    {
                        _analysisContext.Update(Smoothing);
                        _analysisContext.Analyze(_buffer, _analysis, null);
                        ApplyToAvatar();
                    }
                }
                else
                {
                    Array.Clear(_analysis, 0, _analysis.Length);
                }
            }
        }

        public int GetVisemeIndex()
        {
            try
            {
                // This uses the Oculus Viseme Index
                float[] visemes = _analysis;
                (int, float)? biggest = null;
                for (int i = 0; i < visemes.Length; i++)
                {
                    float visemeWeight = visemes[i];
                    if (biggest == null || visemeWeight > biggest.Value.Item2)
                        biggest = (i, visemeWeight);
                }

                if (biggest == null) return -1;
                if (biggest.Value.Item2 <= 0f) return -1;
                return biggest.Value.Item1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public Dictionary<string, float> GetVisemes()
        {
            try
            {
                allVisemes.Clear();
                float[] visemes = _analysis;
                for (int i = 0; i < visemes.Length; i++)
                {
                    string name = Enum.GetNames(typeof(OVRLipSync.Viseme))[i];
                    allVisemes.Add(name, visemes[i]);
                }
                return allVisemes;
            }
            catch (Exception)
            {
                allVisemes.Clear();
                return allVisemes;
            }
        }
        
        private void ApplyToAvatar()
        {
            for (int i = 0; i < (int) CCK.Unity.Interaction.Viseme.Max; i++)
            {
                if (_analysis.Length <= 0 || i >= _analysis.Length) continue;
                BlendshapeDescriptor descriptor =
                    BlendshapeDescriptor.GetDescriptor(b, ac.Avatar.VisemesDict, i);
                if (descriptor == null) continue;
                descriptor.SetWeight(_analysis[i] * 100f);
            }
        }

        public void Dispose()
        {
            _analysisContext?.Dispose();
            _analysisContext = null;
            _ovrLipSync?.Dispose();
            backend?.Dispose();
        }
    }
}