using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Networking.Messages;
using Hypernex.Sandboxing;
using Hypernex.Tools;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Hypernex.Game.Avatar
{
    public abstract class AvatarCreator : IDisposable
    {
        public const string PARAMETER_ID = "parameter";
        public const string BLENDSHAPE_ID = "blendshape";
        public const string MAIN_ANIMATOR = "*main";
        public const string ALL_ANIMATOR_LAYERS = "*all";
        
        public CCK.Unity.Avatar Avatar { get; protected set; }
        public Animator MainAnimator { get; protected set; }
        public FaceTrackingDescriptor FaceTrackingDescriptor { get; protected set; }
        public List<AnimatorPlayable> AnimatorPlayables => new (PlayableAnimators);
        public bool Calibrated { get; protected set; }

        private List<AnimatorControllerParameter> _parameters;
        public List<AnimatorControllerParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    List<AnimatorControllerParameter> allParameters = new();
                    foreach (AnimatorPlayable animatorPlayable in AnimatorPlayables)
                    {
                        List<AnimatorControllerParameter> playableParameters =
                            GetAllParameters(animatorPlayable.AnimatorControllerPlayable);
                        allParameters = allParameters.Concat(playableParameters).ToList();
                    }
                    _parameters = allParameters;
                }
                return _parameters;
            }
        }
        
        protected GameObject HeadAlign;
        internal GameObject VoiceAlign;

        protected readonly RuntimeAnimatorController animatorController =
            Object.Instantiate(Init.Instance.DefaultAvatarAnimatorController);
        private List<AnimatorPlayable> PlayableAnimators = new();
        private List<SkinnedMeshRenderer> skinnedMeshRenderers = new();
        protected List<OVRLipSyncContextMorphTarget> morphTargets = new();
        internal OVRLipSyncContext lipSyncContext;
        internal AudioSource audioSource;
        internal List<Sandbox> localAvatarSandboxes = new();
        protected VRIK vrik;
        internal RotationOffsetDriver headRotator;

        protected readonly Vector3 PelvisTargetLocalPosition = new Vector3(-0.141f, -0.275f, 0.107f);
        protected readonly Quaternion PelvisTargetLocalRotation = Quaternion.identity;

        protected void OnCreate(CCK.Unity.Avatar a, int layer)
        {
            FaceTrackingDescriptor = a.gameObject.GetComponent<FaceTrackingDescriptor>();
            a.gameObject.AddComponent<AvatarBehaviour>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in a.gameObject
                         .GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderer.updateWhenOffscreen = true;
                skinnedMeshRenderers.Add(skinnedMeshRenderer);
            }
            foreach (MaterialDescriptor materialDescriptor in a.transform.GetComponentsInChildren<MaterialDescriptor>())
                materialDescriptor.SetMaterials(AssetBundleTools.Platform);
            foreach (Transform transform in a.transform.GetComponentsInChildren<Transform>())
                transform.gameObject.layer = layer;
            Animator an = a.transform.GetComponent<Animator>();
            if(an != null)
                an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if(MainAnimator == null || MainAnimator.avatar == null) return;
            MainAnimator.runtimeAnimatorController = null;
            MainAnimator.applyRootMotion = false;
            Transform head = MainAnimator.GetBoneTransform(HumanBodyBones.Head);
            if(head == null) return;
            headRotator = new RotationOffsetDriver(head, a.transform);
        }

        protected void DriveCamera(Transform cam)
        {
            if(vrik != null || headRotator == null) return;
            headRotator.Rotate(cam.rotation);
        }
        
        protected void SetupAnimators()
        {
            foreach (CustomPlayableAnimator customPlayableAnimator in Avatar.Animators)
            {
                if (customPlayableAnimator == null || customPlayableAnimator.AnimatorController == null) continue;
                if (customPlayableAnimator.AnimatorOverrideController != null)
                    customPlayableAnimator.AnimatorOverrideController.runtimeAnimatorController =
                        customPlayableAnimator.AnimatorController;
                PlayableGraph playableGraph = PlayableGraph.Create(customPlayableAnimator.AnimatorController.name);
                AnimatorControllerPlayable animatorControllerPlayable =
                    AnimatorControllerPlayable.Create(playableGraph, customPlayableAnimator.AnimatorController);
                PlayableOutput playableOutput = AnimationPlayableOutput.Create(playableGraph,
                    customPlayableAnimator.AnimatorController.name, MainAnimator);
                playableOutput.SetSourcePlayable(animatorControllerPlayable);
                PlayableAnimators.Add(new AnimatorPlayable
                {
                    CustomPlayableAnimator = customPlayableAnimator,
                    PlayableGraph = playableGraph,
                    AnimatorControllerPlayable = animatorControllerPlayable,
                    PlayableOutput = playableOutput,
                    AnimatorControllerParameters = GetAllParameters(animatorControllerPlayable)
                });
                playableGraph.Play();
            }
        }
        
        // Here's an idea Unity.. EXPOSE THE PARAMETERS??
        private List<AnimatorControllerParameter> GetAllParameters(AnimatorControllerPlayable animatorControllerPlayable)
        {
            List<AnimatorControllerParameter> parameters = new();
            bool c = true;
            int i = 0;
            while (c)
            {
                try
                {
                    AnimatorControllerParameter animatorControllerParameter =
                        animatorControllerPlayable.GetParameter(i);
                    parameters.Add(animatorControllerParameter);
                    i++;
                }
                catch (IndexOutOfRangeException) {c = false;}
            }
            return parameters;
        }

        protected AnimatorControllerParameter GetParameterByName(string name, AnimatorPlayable animatorPlayable)
        {
            foreach (AnimatorControllerParameter animatorPlayableAnimatorControllerParameter in animatorPlayable.AnimatorControllerParameters)
            {
                if (animatorPlayableAnimatorControllerParameter.name == name)
                    return animatorPlayableAnimatorControllerParameter;
            }
            return null;
        }

        protected AnimatorPlayable? GetPlayable(CustomPlayableAnimator customPlayableAnimator) =>
            AnimatorPlayables.Find(x => x.CustomPlayableAnimator == customPlayableAnimator);

        public T GetParameter<T>(string parameterName, CustomPlayableAnimator target = null)
        {
            if (target != null)
            {
                AnimatorPlayable? animatorPlayable = GetPlayable(target);
                if (animatorPlayable != null)
                {
                    switch (Type.GetTypeCode(typeof(T)))
                    {
                        case TypeCode.Boolean:
                            return (T) Convert.ChangeType(
                                animatorPlayable.Value.AnimatorControllerPlayable.GetBool(parameterName), typeof(T));
                        case TypeCode.Int32:
                            return (T) Convert.ChangeType(
                                animatorPlayable.Value.AnimatorControllerPlayable.GetInteger(parameterName), typeof(T));
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return (T) Convert.ChangeType(
                                animatorPlayable.Value.AnimatorControllerPlayable.GetFloat(parameterName), typeof(T));
                        default:
                            if(typeof(T) == typeof(float))
                                return (T) Convert.ChangeType(
                                    animatorPlayable.Value.AnimatorControllerPlayable.GetFloat(parameterName), typeof(T));
                            return default;
                    }
                }
            }
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
            {
                if (playableAnimator.AnimatorControllerParameters.Count(x => x.name == parameterName) > 0)
                {
                    switch (Type.GetTypeCode(typeof(T)))
                    {
                        case TypeCode.Boolean:
                            return (T) Convert.ChangeType(
                                playableAnimator.AnimatorControllerPlayable.GetBool(parameterName), typeof(T));
                        case TypeCode.Int32:
                            return (T) Convert.ChangeType(
                                playableAnimator.AnimatorControllerPlayable.GetInteger(parameterName), typeof(T));
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return (T) Convert.ChangeType(
                                playableAnimator.AnimatorControllerPlayable.GetFloat(parameterName), typeof(T));
                        default:
                            if(typeof(T) == typeof(float))
                                return (T) Convert.ChangeType(
                                    playableAnimator.AnimatorControllerPlayable.GetFloat(parameterName), typeof(T));
                            return default;
                    }
                }
            }
            return default;
        }
        
        public object GetParameter(string parameterName, CustomPlayableAnimator target = null)
        {
            if (target != null)
            {
                AnimatorPlayable? animatorPlayable = GetPlayable(target);
                if (animatorPlayable != null)
                {
                    AnimatorControllerParameter animatorControllerParameter =
                        GetParameterByName(parameterName, animatorPlayable.Value);
                    if (animatorControllerParameter != null)
                    {
                        switch (animatorControllerParameter.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                return animatorPlayable.Value.AnimatorControllerPlayable.GetBool(parameterName);
                            case AnimatorControllerParameterType.Int:
                                return animatorPlayable.Value.AnimatorControllerPlayable.GetInteger(parameterName);
                            case AnimatorControllerParameterType.Float:
                                return animatorPlayable.Value.AnimatorControllerPlayable.GetFloat(parameterName);
                        }
                    }
                }
            }
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
            {
                AnimatorControllerParameter animatorControllerParameter =
                    GetParameterByName(parameterName, playableAnimator);
                if (animatorControllerParameter != null)
                {
                    switch (animatorControllerParameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            return playableAnimator.AnimatorControllerPlayable.GetBool(parameterName);
                        case AnimatorControllerParameterType.Int:
                            return playableAnimator.AnimatorControllerPlayable.GetInteger(parameterName);
                        case AnimatorControllerParameterType.Float:
                            return playableAnimator.AnimatorControllerPlayable.GetFloat(parameterName);
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// Sets the value of a Parameter
        /// </summary>
        /// <param name="parameterName">The name of the Parameter</param>
        /// <param name="value">The value to set the parameter to</param>
        /// <param name="target">The target Playable, null for all</param>
        /// <param name="force">Forces the value to be casted to all types</param>
        /// <param name="mainAnimator">Drive the parameter for the MainAnimator</param>
        /// <typeparam name="T">A valid parameter type (float, int, or bool)</typeparam>
        public void SetParameter<T>(string parameterName, T value, CustomPlayableAnimator target = null, 
            bool force = false, bool mainAnimator = false)
        {
            if (mainAnimator)
            {
                try
                {
                    AnimatorControllerParameter animatorControllerParameter =
                        MainAnimator.parameters.First(x => x.name == parameterName);
                    switch (animatorControllerParameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            MainAnimator.SetBool(parameterName, (bool) Convert.ChangeType(value, typeof(bool)));
                            break;
                        case AnimatorControllerParameterType.Int:
                            MainAnimator.SetInteger(parameterName, (int) Convert.ChangeType(value, typeof(int)));
                            break;
                        case AnimatorControllerParameterType.Float:
                            MainAnimator.SetFloat(parameterName, (float) Convert.ChangeType(value, typeof(float)));
                            break;
                    }
                }catch(Exception){}
            }
            if (target != null)
            {
                AnimatorPlayable? animatorPlayable = GetPlayable(target);
                if (animatorPlayable != null)
                {
                    AnimatorControllerParameter animatorControllerParameter =
                        GetParameterByName(parameterName, animatorPlayable.Value);
                    if (animatorControllerParameter != null)
                    {
                        switch (animatorControllerParameter.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                animatorPlayable.Value.AnimatorControllerPlayable.SetBool(parameterName,
                                    (bool) Convert.ChangeType(value, typeof(bool)));
                                break;
                            case AnimatorControllerParameterType.Int:
                                animatorPlayable.Value.AnimatorControllerPlayable.SetInteger(parameterName,
                                    (int) Convert.ChangeType(value, typeof(int)));
                                break;
                            case AnimatorControllerParameterType.Float:
                                animatorPlayable.Value.AnimatorControllerPlayable.SetFloat(parameterName,
                                    (float) Convert.ChangeType(value, typeof(float)));
                                break;
                        }
                    }
                }
                return;
            }
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
            {
                if (force)
                {
                    foreach (AnimatorControllerParameter animatorControllerParameter in playableAnimator
                                 .AnimatorControllerParameters.Where(x => x.name == parameterName))
                    {
                        switch (animatorControllerParameter.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                playableAnimator.AnimatorControllerPlayable.SetBool(parameterName,
                                    (bool) Convert.ChangeType(value, typeof(bool)));
                                break;
                            case AnimatorControllerParameterType.Int:
                                playableAnimator.AnimatorControllerPlayable.SetInteger(parameterName,
                                    (int) Convert.ChangeType(value, typeof(int)));
                                break;
                            case AnimatorControllerParameterType.Float:
                                playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName,
                                    (float) Convert.ChangeType(value, typeof(float)));
                                break;
                        }
                    }
                }
                else
                {
                    if (playableAnimator.AnimatorControllerParameters.Count(x => x.name == parameterName) > 0)
                    {
                        switch (Type.GetTypeCode(typeof(T)))
                        {
                            case TypeCode.Boolean:
                                playableAnimator.AnimatorControllerPlayable.SetBool(parameterName,
                                    (bool) Convert.ChangeType(value, typeof(bool)));
                                break;
                            case TypeCode.Int32:
                                playableAnimator.AnimatorControllerPlayable.SetInteger(parameterName,
                                    (int) Convert.ChangeType(value, typeof(int)));
                                break;
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName,
                                    (float) Convert.ChangeType(value, typeof(float)));
                                break;
                            default:
                                if (typeof(T) == typeof(float))
                                    playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName,
                                        (float) Convert.ChangeType(value, typeof(float)));
                                break;
                        }
                    }
                }
            }
        }

        internal List<WeightedObjectUpdate> GetAnimatorWeights()
        {
            List<WeightedObjectUpdate> weights = new();
            foreach (AnimatorControllerParameter animatorControllerParameter in MainAnimator.parameters)
            {
                float weight;
                switch (animatorControllerParameter.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        weight = MainAnimator.GetBool(animatorControllerParameter.name) ? 1f : 0f;
                        break;
                    case AnimatorControllerParameterType.Int:
                        weight = MainAnimator.GetInteger(animatorControllerParameter.name);
                        break;
                    case AnimatorControllerParameterType.Float:
                        weight = MainAnimator.GetFloat(animatorControllerParameter.name);
                        break;
                    default:
                        continue;
                }
                WeightedObjectUpdate weightedObjectUpdate = new WeightedObjectUpdate
                {
                    PathToWeightContainer = MAIN_ANIMATOR,
                    TypeOfWeight = PARAMETER_ID,
                    WeightIndex = animatorControllerParameter.name,
                    Weight = weight
                };
                weights.Add(weightedObjectUpdate);
            }
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter playableAnimatorControllerParameter in playableAnimator.AnimatorControllerParameters)
                {
                    WeightedObjectUpdate weightedObjectUpdate = new WeightedObjectUpdate
                    {
                        PathToWeightContainer = playableAnimator.CustomPlayableAnimator.AnimatorController.name,
                        TypeOfWeight = PARAMETER_ID,
                        WeightIndex = playableAnimatorControllerParameter.name
                    };
                    switch (playableAnimatorControllerParameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            weightedObjectUpdate.Weight =
                                playableAnimator.AnimatorControllerPlayable.GetBool(playableAnimatorControllerParameter
                                    .name)
                                    ? 1.00f
                                    : 0.00f;
                            break;
                        case AnimatorControllerParameterType.Int:
                            weightedObjectUpdate.Weight =
                                playableAnimator.AnimatorControllerPlayable.GetInteger(
                                    playableAnimatorControllerParameter.name);
                            break;
                        case AnimatorControllerParameterType.Float:
                            weightedObjectUpdate.Weight =
                                playableAnimator.AnimatorControllerPlayable.GetFloat(playableAnimatorControllerParameter
                                    .name);
                            break;
                        default:
                            continue;
                    }
                    weights.Add(weightedObjectUpdate);
                }
            }
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            {
                PathDescriptor p = skinnedMeshRenderer.gameObject.GetComponent<PathDescriptor>();
                if(p == null) continue;
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                {
                    // Exclude Visemes
                    if (Avatar.UseVisemes && Avatar.VisemesDict.Count(x =>
                            x.Value.SkinnedMeshRenderer == skinnedMeshRenderer && x.Value.BlendshapeIndex == i) >
                        0) continue;
                    // Exclude ShadowClones
                    if(skinnedMeshRenderer.gameObject.name.Contains("shadowclone_")) continue;
                    float w = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    WeightedObjectUpdate weightedObjectUpdate = new WeightedObjectUpdate
                    {
                        PathToWeightContainer = p.path,
                        TypeOfWeight = BLENDSHAPE_ID,
                        WeightIndex = i.ToString(),
                        Weight = w
                    };
                    weights.Add(weightedObjectUpdate);
                }
            }
            return weights;
        }
        
        public void GetBlendshapeWeight(SkinnedMeshRenderer skinnedMeshRenderer, int blendshapeIndex) =>
            skinnedMeshRenderer.GetBlendShapeWeight(blendshapeIndex);

        public void SetBlendshapeWeight(SkinnedMeshRenderer skinnedMeshRenderer, int blendshapeIndex, float weight) =>
            skinnedMeshRenderer.SetBlendShapeWeight(blendshapeIndex, weight);
        
        public Transform GetBoneFromHumanoid(HumanBodyBones humanBodyBones)
        {
            if (MainAnimator == null)
                return null;
            return MainAnimator.GetBoneTransform(humanBodyBones);
        }
        
        internal void ApplyAudioClipToLipSync(float[] data)
        {
            if (lipSyncContext == null)
                return;
            lipSyncContext.PreprocessAudioSamples(data, (int) Mic.NumChannels);
            lipSyncContext.ProcessAudioSamples(data, (int) Mic.NumChannels);
            lipSyncContext.PostprocessAudioSamples(data, (int) Mic.NumChannels);
        }
        
        protected OVRLipSyncContextMorphTarget GetMorphTargetBySkinnedMeshRenderer(
            SkinnedMeshRenderer skinnedMeshRenderer)
        {
            foreach (OVRLipSyncContextMorphTarget morphTarget in new List<OVRLipSyncContextMorphTarget>(morphTargets))
            {
                if (morphTarget == null)
                    morphTargets.Remove(morphTarget);
                else if (morphTarget.skinnedMeshRenderer == skinnedMeshRenderer)
                    return morphTarget;
            }
            OVRLipSyncContextMorphTarget m = VoiceAlign.AddComponent<OVRLipSyncContextMorphTarget>();
            m.skinnedMeshRenderer = skinnedMeshRenderer;
            morphTargets.Add(m);
            return m;
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

        /// <summary>
        /// Gets all Visemes and their values
        /// </summary>
        /// <returns>Key: Name of Viseme, Value: Weight of Viseme</returns>
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

        protected void SetVisemeAsBlendshape(ref OVRLipSyncContextMorphTarget morphTarget, Viseme viseme,
            BlendshapeDescriptor blendshapeDescriptor)
        {
            int indexToInsert = (int) viseme;
            int[] currentBlendshapes = new int[15];
            Array.Copy(morphTarget.visemeToBlendTargets, currentBlendshapes, 15);
            currentBlendshapes[indexToInsert] = blendshapeDescriptor.BlendshapeIndex;
            morphTarget.visemeToBlendTargets = currentBlendshapes;
        }
        
        protected void RelaxWrists(Transform leftLowerArm, Transform rightLowerArm, Transform leftHand,
            Transform rightHand)
        {
            if (leftLowerArm == null || rightLowerArm == null || leftHand == null || rightHand == null)
                return;
            TwistRelaxer twistRelaxer = Avatar.gameObject.AddComponent<TwistRelaxer>();
            twistRelaxer.ik = vrik;
            TwistSolver leftSolver = new TwistSolver { transform = leftLowerArm, children = new []{leftHand} };
            TwistSolver rightSolver = new TwistSolver { transform = rightLowerArm, children = new []{rightHand} };
            twistRelaxer.twistSolvers = new[] { leftSolver, rightSolver };
        }
        
        public Transform GetTargetChild(Transform tracker)
        {
            for (int i = 0; i < tracker.childCount; i++)
            {
                Transform t = tracker.GetChild(0);
                if (t.gameObject.name.Contains("Target")) return t;
            }
            return null;
        }
        
        internal void FixedUpdate() => localAvatarSandboxes.ForEach(x => x.Runtime.FixedUpdate());

        internal void Update()
        {
            SetParameter("Viseme", GetVisemeIndex(), null, true);
            foreach (KeyValuePair<string, float> viseme in GetVisemes())
                SetParameter(viseme.Key, viseme.Value, null, true);
            localAvatarSandboxes.ForEach(x => x.Runtime.Update());
        }
        
        internal void LateUpdate() => localAvatarSandboxes.ForEach(x => x.Runtime.LateUpdate());

        public virtual void Dispose()
        {
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
                try
                {
                    playableAnimator.PlayableGraph.Destroy();
                }
                catch(ArgumentException){}
            foreach (Sandbox localAvatarSandbox in new List<Sandbox>(localAvatarSandboxes))
            {
                localAvatarSandboxes.Remove(localAvatarSandbox);
                localAvatarSandbox.Dispose();
            }
            Object.Destroy(Avatar.gameObject);
        }
        
        public class AvatarBehaviour : MonoBehaviour
        {
            private void OnFootstep(AnimationEvent animationEvent)
            {
            
            }

            private void OnLand(AnimationEvent animationEvent)
            {
            
            }
        }
    }
}