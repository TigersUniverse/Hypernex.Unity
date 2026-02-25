using System;
using System.Collections.Generic;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.CCK.Unity.Interaction;
using Hypernex.Tools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.Game.Avatar.VisemeProviders
{
    public class OculusVisemeProvider : IVisemeProvider
    {
        protected List<OVRLipSyncContextMorphTarget> morphTargets = new();
        internal OVRLipSyncContext lipSyncContext;
        private bool enabled;

        bool IVisemeProvider.Enabled
        {
            get => lipSyncContext != null && lipSyncContext.enabled;
            set
            {
                if(lipSyncContext == null) return;
                lipSyncContext.enabled = value;
            }
        }

        void IVisemeProvider.SetupLocal(AvatarCreator avatarCreator, BlendshapeDescriptor[] blendshapes)
        {
            lipSyncContext = avatarCreator.VoiceAlign.AddComponent<OVRLipSyncContext>();
            lipSyncContext.audioSource = avatarCreator.audioSource;
            lipSyncContext.enableKeyboardInput = false;
            lipSyncContext.enableTouchInput = false;
            lipSyncContext.skipAudioSource = true;
            morphTargets.Clear();
            for (int i = 0; i < (int) Viseme.Max; i++)
            {
                BlendshapeDescriptor descriptor =
                    BlendshapeDescriptor.GetDescriptor(blendshapes, avatarCreator.Avatar.VisemesDict, i);
                if (descriptor == null) continue;
                var morphTarget =
                    GetMorphTargetBySkinnedMeshRenderer(avatarCreator, descriptor.SkinnedMeshRenderer);
                SetVisemeAsBlendshape(ref morphTarget, (Viseme) i, descriptor);
            }
        }

        void IVisemeProvider.SetupNet(AvatarCreator avatarCreator, BlendshapeDescriptor[] blendshapes)
        {
            lipSyncContext = avatarCreator.VoiceAlign.AddComponent<OVRLipSyncContext>();
            lipSyncContext.audioSource = avatarCreator.audioSource;
            lipSyncContext.enableKeyboardInput = false;
            lipSyncContext.enableTouchInput = false;
            lipSyncContext.audioLoopback = true;
            morphTargets.Clear();
            for (int i = 0; i < (int) Viseme.Max; i++)
            {
                BlendshapeDescriptor descriptor = BlendshapeDescriptor.GetDescriptor(blendshapes, avatarCreator.Avatar.VisemesDict, i);
                if (descriptor == null) continue;
                var morphTarget = GetMorphTargetBySkinnedMeshRenderer(avatarCreator, descriptor.SkinnedMeshRenderer);
                SetVisemeAsBlendshape(ref morphTarget, (Viseme) i, descriptor);
            }
        }

        void IVisemeProvider.ApplyLocal(float[] data)
        {
            if (lipSyncContext == null)
                return;
            lipSyncContext.ProcessAudioSamples(data, Mic.NumChannels);
        }

        /// <summary>
        /// Gets the current Index for the current Viseme using the Oculus Viseme Index
        /// </summary>
        /// <returns></returns>
        public int GetVisemeIndex()
        {
            try
            {
                // This uses the Oculus Viseme Index
                float[] visemes = lipSyncContext.GetCurrentPhonemeFrame()?.Visemes ?? Array.Empty<float>();
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
                Dictionary<string, float> allVisemes = new();
                float[] visemes = lipSyncContext.GetCurrentPhonemeFrame()?.Visemes ?? Array.Empty<float>();
                for (int i = 0; i < visemes.Length; i++)
                {
                    string name = Enum.GetNames(typeof(OVRLipSync.Viseme))[i];
                    allVisemes.Add(name, visemes[i]);
                }

                return allVisemes;
            }
            catch (Exception)
            {
                return new Dictionary<string, float>();
            }
        }
        
        protected OVRLipSyncContextMorphTarget GetMorphTargetBySkinnedMeshRenderer(
            AvatarCreator avatarCreator, SkinnedMeshRenderer skinnedMeshRenderer)
        {
            foreach (OVRLipSyncContextMorphTarget morphTarget in new List<OVRLipSyncContextMorphTarget>(morphTargets))
            {
                if (morphTarget == null)
                    morphTargets.Remove(morphTarget);
                else if (morphTarget.skinnedMeshRenderer == skinnedMeshRenderer)
                    return morphTarget;
            }
            OVRLipSyncContextMorphTarget m = avatarCreator.VoiceAlign.AddComponent<OVRLipSyncContextMorphTarget>();
            m.skinnedMeshRenderer = skinnedMeshRenderer;
            morphTargets.Add(m);
            return m;
        }

        protected void SetVisemeAsBlendshape(ref OVRLipSyncContextMorphTarget morphTarget, Viseme viseme,
            BlendshapeDescriptor blendshapeDescriptor)
        {
            int indexToInsert = (int) viseme;
            int[] currentBlendshapes = new int[15];
            Array.Copy(morphTarget.visemeToBlendTargets, currentBlendshapes, 15);
            currentBlendshapes[indexToInsert] = blendshapeDescriptor.BlendshapeIndex;
            morphTarget.visemeToBlendTargets = currentBlendshapes;
        }

        public void Dispose()
        {
            if (lipSyncContext != null) Object.Destroy(lipSyncContext);
        }
    }
}