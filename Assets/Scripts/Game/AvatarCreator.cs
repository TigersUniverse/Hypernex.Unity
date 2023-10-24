using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game.Bindings;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Sandboxing;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using VRCFaceTracking.Core.Params.Data;
using Avatar = Hypernex.CCK.Unity.Avatar;
using Object = UnityEngine.Object;

namespace Hypernex.Game
{
    public class AvatarCreator : IDisposable
    {
        public const string PARAMETER_ID = "parameter";
        public const string BLENDSHAPE_ID = "blendshape";
        
        public Avatar Avatar;
        public Animator MainAnimator;
        public FaceTrackingDescriptor FaceTrackingDescriptor;
        public List<AnimatorPlayable> AnimatorPlayables => new(PlayableAnimators);
        public bool calibrated { get; private set; }

        private List<AnimatorPlayable> PlayableAnimators = new();
        internal List<Sandbox> localAvatarSandboxes = new();
        private VRIK vrik;
        private VRIKCalibrator.Settings vrikSettings = new()
        {
            scaleMlp = 0.9f,
            handOffset = new Vector3(0, 0.01f, -0.1f),
        };
        private GameObject headAlign;
        internal GameObject voiceAlign;
        internal AudioSource audioSource;
        private List<AvatarNearClip> avatarNearClips = new();
        internal OVRLipSyncContext lipSyncContext;
        private List<OVRLipSyncContextMorphTarget> morphTargets = new();
        private List<SkinnedMeshRenderer> skinnedMeshRenderers = new();
        private FingerCalibration fingerCalibration;

        public AvatarCreator(LocalPlayer localPlayer, Avatar a, bool isVR)
        {
            a = Object.Instantiate(a.gameObject).GetComponent<Avatar>();
            a.gameObject.AddComponent<AvatarBehaviour>();
            Avatar = a;
            SceneManager.MoveGameObjectToScene(a.gameObject, localPlayer.gameObject.scene);
            MainAnimator = a.GetComponent<Animator>();
            fingerCalibration = new FingerCalibration(this);
            headAlign = new GameObject("headalign_" + Guid.NewGuid());
            headAlign.transform.SetParent(a.ViewPosition.transform);
            headAlign.transform.SetLocalPositionAndRotation(Vector3.zero, new Quaternion(0,0,0,0));
            voiceAlign = new GameObject("voicealign_" + Guid.NewGuid());
            voiceAlign.transform.SetParent(a.SpeechPosition.transform);
            voiceAlign.transform.SetLocalPositionAndRotation(Vector3.zero, new Quaternion(0,0,0,0));
            audioSource = voiceAlign.AddComponent<AudioSource>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in
                     a.transform.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (!skinnedMeshRenderer.name.Contains("shadowclone_"))
                {
                    AvatarNearClip avatarNearClip = skinnedMeshRenderer.gameObject.AddComponent<AvatarNearClip>();
                    if(avatarNearClip != null && avatarNearClip.Setup(this, localPlayer.Camera))
                        avatarNearClips.Add(avatarNearClip);
                }
            avatarNearClips.ForEach(x => x.CreateShadows());
            FaceTrackingDescriptor = Avatar.gameObject.GetComponent<FaceTrackingDescriptor>();
            Transform head = GetBoneFromHumanoid(HumanBodyBones.Head);
            if(head != null)
                vrikSettings.headOffset = head.position - headAlign.transform.position;
            a.gameObject.name = "avatar";
            a.transform.SetParent(localPlayer.transform);
            if(isVR)
                a.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
            else
                a.transform.SetLocalPositionAndRotation(new Vector3(0, -(a.transform.localScale.y * 0.75f), 0), new Quaternion(0, 0, 0, 0));
            a.transform.localScale = Vector3.one;
            if (isVR)
            {
                vrik = Avatar.gameObject.AddComponent<VRIK>();
                for (int i = 0; i < LocalPlayer.Instance.Camera.transform.childCount; i++)
                {
                    Transform child = LocalPlayer.Instance.Camera.transform.GetChild(i);
                    if (child.name == "Head Target")
                        Object.Destroy(child.gameObject);
                }
                for (int i = 0; i < LocalPlayer.Instance.LeftHandVRIKTarget.childCount; i++)
                {
                    Transform child = LocalPlayer.Instance.LeftHandVRIKTarget.GetChild(i);
                    Object.Destroy(child.gameObject);
                }
                for (int i = 0; i < LocalPlayer.Instance.RightHandVRIKTarget.childCount; i++)
                {
                    Transform child = LocalPlayer.Instance.RightHandVRIKTarget.GetChild(i);
                    Object.Destroy(child.gameObject);
                }
                if (!XRTracker.CanFBT)
                {
                    RelaxWrists(GetBoneFromHumanoid(HumanBodyBones.LeftLowerArm),
                        GetBoneFromHumanoid(HumanBodyBones.RightLowerArm), GetBoneFromHumanoid(HumanBodyBones.LeftHand),
                        GetBoneFromHumanoid(HumanBodyBones.RightHand));
                }
            }
            else
            {
                SetupAnimators();
                calibrated = true;
            }
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in Avatar.gameObject
                         .GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderer.updateWhenOffscreen = true;
                skinnedMeshRenderers.Add(skinnedMeshRenderer);
            }
            if (string.IsNullOrEmpty(LocalPlayer.Instance.avatarMeta.ImageURL))
                CurrentAvatarBanner.Instance.Render(this, Array.Empty<byte>());
            else
                DownloadTools.DownloadBytes(LocalPlayer.Instance.avatarMeta.ImageURL,
                    bytes => CurrentAvatarBanner.Instance.Render(this, bytes));
            SetupLipSyncLocalPlayer();
            InitMaterialDescriptors(a.transform);
            SetLayer(7);
            CompleteBoth();
        }

        public AvatarCreator(NetPlayer np, Avatar a)
        {
            a = Object.Instantiate(a.gameObject).GetComponent<Avatar>();
            Avatar = a;
            SceneManager.MoveGameObjectToScene(a.gameObject, np.gameObject.scene);
            MainAnimator = a.GetComponent<Animator>();
            MainAnimator.runtimeAnimatorController = null;
            voiceAlign = new GameObject("voicealign_" + Guid.NewGuid());
            voiceAlign.transform.SetParent(a.SpeechPosition.transform);
            voiceAlign.transform.SetLocalPositionAndRotation(Vector3.zero, new Quaternion(0,0,0,0));
            audioSource = voiceAlign.AddComponent<AudioSource>();
            audioSource.spatialize = true;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 0;
            audioSource.maxDistance = 10;
            audioSource.outputAudioMixerGroup = Init.Instance.VoiceGroup;
            if(np.nameplateTemplate != null)
                np.nameplateTemplate.OnNewAvatar(this);
            a.transform.SetParent(np.transform);
            a.gameObject.name = "avatar";
            a.transform.SetLocalPositionAndRotation(new Vector3(0, -1, 0), new Quaternion(0, 0, 0, 0));
            foreach (CustomPlayableAnimator customPlayableAnimator in a.Animators)
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
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in Avatar.gameObject
                         .GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderer.updateWhenOffscreen = true;
                skinnedMeshRenderers.Add(skinnedMeshRenderer);
            }
#if DYNAMIC_BONE
            foreach (DynamicBone dynamicBone in Avatar.transform.GetComponentsInChildren<DynamicBone>())
            {
                dynamicBone.m_UpdateMode = DynamicBone.UpdateMode.AnimatePhysics;
            }
#endif
            SetupLipSyncNetPlayer();
            InitMaterialDescriptors(a.transform);
            SetLayer(10);
            CompleteBoth();
        }

        private void CompleteBoth()
        {
            Animator an = Avatar.transform.GetComponent<Animator>();
            an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private void SetupAnimators()
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

        private void SetLayer(int layer)
        {
            foreach (Transform transform in Avatar.transform.GetComponentsInChildren<Transform>())
                transform.gameObject.layer = layer;
        }

        private OVRLipSyncContextMorphTarget GetMorphTargetBySkinnedMeshRenderer(
            SkinnedMeshRenderer skinnedMeshRenderer)
        {
            foreach (OVRLipSyncContextMorphTarget morphTarget in new List<OVRLipSyncContextMorphTarget>(morphTargets))
            {
                if (morphTarget == null)
                    morphTargets.Remove(morphTarget);
                else if (morphTarget.skinnedMeshRenderer == skinnedMeshRenderer)
                    return morphTarget;
            }
            OVRLipSyncContextMorphTarget m = voiceAlign.AddComponent<OVRLipSyncContextMorphTarget>();
            m.skinnedMeshRenderer = skinnedMeshRenderer;
            morphTargets.Add(m);
            return m;
        }

        private void InitMaterialDescriptors(Transform av)
        {
            foreach (MaterialDescriptor materialDescriptor in av.GetComponentsInChildren<MaterialDescriptor>())
                materialDescriptor.SetMaterials(AssetBundleTools.Platform);
        }

        private void SetVisemeAsBlendshape(ref OVRLipSyncContextMorphTarget morphTarget, Viseme viseme,
            BlendshapeDescriptor blendshapeDescriptor)
        {
            int indexToInsert = (int) viseme;
            int[] currentBlendshapes = new int[15];
            Array.Copy(morphTarget.visemeToBlendTargets, currentBlendshapes, 15);
            currentBlendshapes[indexToInsert] = blendshapeDescriptor.BlendshapeIndex;
            morphTarget.visemeToBlendTargets = currentBlendshapes;
        }

        private void SetupLipSyncNetPlayer()
        {
            if (!Avatar.UseVisemes) return;
            lipSyncContext = voiceAlign.AddComponent<OVRLipSyncContext>();
            lipSyncContext.audioSource = audioSource;
            lipSyncContext.enableKeyboardInput = false;
            lipSyncContext.enableTouchInput = false;
            lipSyncContext.audioLoopback = true;
            morphTargets.Clear();
            foreach (KeyValuePair<Viseme, BlendshapeDescriptor> avatarVisemeRenderer in Avatar.VisemesDict)
            {
                OVRLipSyncContextMorphTarget morphTarget =
                    GetMorphTargetBySkinnedMeshRenderer(avatarVisemeRenderer.Value.SkinnedMeshRenderer);
                SetVisemeAsBlendshape(ref morphTarget, avatarVisemeRenderer.Key, avatarVisemeRenderer.Value);
            }
        }

        internal void ApplyAudioClipToLipSync(float[] data)
        {
            if (lipSyncContext == null)
                return;
            lipSyncContext.PreprocessAudioSamples(data, (int) Mic.NumChannels);
            lipSyncContext.ProcessAudioSamples(data, (int) Mic.NumChannels);
            lipSyncContext.PostprocessAudioSamples(data, (int) Mic.NumChannels);
        }

        private void SetupLipSyncLocalPlayer()
        {
            if (!Avatar.UseVisemes) return;
            lipSyncContext = voiceAlign.AddComponent<OVRLipSyncContext>();
            lipSyncContext.audioSource = audioSource;
            lipSyncContext.enableKeyboardInput = false;
            lipSyncContext.enableTouchInput = false;
            morphTargets.Clear();
            foreach (KeyValuePair<Viseme, BlendshapeDescriptor> avatarVisemeRenderer in Avatar.VisemesDict)
            {
                OVRLipSyncContextMorphTarget morphTarget =
                    GetMorphTargetBySkinnedMeshRenderer(avatarVisemeRenderer.Value.SkinnedMeshRenderer);
                SetVisemeAsBlendshape(ref morphTarget, avatarVisemeRenderer.Key, avatarVisemeRenderer.Value);
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

        private AnimatorControllerParameter GetParameterByName(string name, AnimatorPlayable animatorPlayable)
        {
            foreach (AnimatorControllerParameter animatorPlayableAnimatorControllerParameter in animatorPlayable.AnimatorControllerParameters)
            {
                if (animatorPlayableAnimatorControllerParameter.name == name)
                    return animatorPlayableAnimatorControllerParameter;
            }
            return null;
        }

        private AnimatorPlayable? GetPlayable(CustomPlayableAnimator customPlayableAnimator) =>
            PlayableAnimators.Find(x => x.CustomPlayableAnimator == customPlayableAnimator);

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
            foreach (AnimatorPlayable playableAnimator in PlayableAnimators)
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
            foreach (AnimatorPlayable playableAnimator in PlayableAnimators)
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

        public void SetParameter<T>(string parameterName, T value, CustomPlayableAnimator target = null)
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
            foreach (AnimatorPlayable playableAnimator in PlayableAnimators)
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
                            if(typeof(T) == typeof(float))
                                playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName,
                                    (float) Convert.ChangeType(value, typeof(float)));
                            break;
                    }
                }
            }
        }

        internal List<WeightedObjectUpdate> GetAnimatorWeights(JoinAuth j)
        {
            List<WeightedObjectUpdate> weights = new();
            foreach (AnimatorPlayable playableAnimator in PlayableAnimators)
            {
                foreach (AnimatorControllerParameter playableAnimatorControllerParameter in playableAnimator.AnimatorControllerParameters)
                {
                    WeightedObjectUpdate weightedObjectUpdate = new WeightedObjectUpdate
                    {
                        Auth = j,
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
                    float w = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    WeightedObjectUpdate weightedObjectUpdate = new WeightedObjectUpdate
                    {
                        Auth = j,
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

        internal void HandleNetParameter(WeightedObjectUpdate weight)
        {
            switch (weight.TypeOfWeight.ToLower())
            {
                case PARAMETER_ID:
                {
                    string parameterName = weight.WeightIndex;
                    foreach (AnimatorPlayable playableAnimator in PlayableAnimators)
                    {
                        if (playableAnimator.CustomPlayableAnimator.AnimatorController.name !=
                            weight.PathToWeightContainer) continue;
                        try
                        {
                            AnimatorControllerParameter parameter = GetParameterByName(parameterName, playableAnimator);
                            if (parameter != null)
                            {
                                switch (parameter.type)
                                {
                                    case AnimatorControllerParameterType.Bool:
                                        playableAnimator.AnimatorControllerPlayable.SetBool(parameterName,
                                            Math.Abs(weight.Weight - 1.00f) < 0.01);
                                        break;
                                    case AnimatorControllerParameterType.Int:
                                        playableAnimator.AnimatorControllerPlayable.SetInteger(parameterName,
                                            (int) weight.Weight);
                                        break;
                                    case AnimatorControllerParameterType.Float:
                                        playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName,
                                            weight.Weight);
                                        break;
                                }
                            }
                        } catch(Exception){}
                    }
                    break;
                }
                case BLENDSHAPE_ID:
                {
                    try
                    {
                        Transform t = Avatar.transform.parent.Find(weight.PathToWeightContainer);
                        if (t == null) break;
                        SkinnedMeshRenderer s = t.gameObject.GetComponent<SkinnedMeshRenderer>();
                        if (s == null) break;
                        s.SetBlendShapeWeight(Convert.ToInt32(weight.WeightIndex), weight.Weight);
                    } catch(Exception){}
                    break;
                }
            }
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

        private void RelaxWrists(Transform leftLowerArm, Transform rightLowerArm, Transform leftHand,
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

        /// <summary>
        /// Sorts Trackers from 0 by how close they are to the Body, LeftFoot, and RightFoot
        /// </summary>
        /// <returns>Sorted Tracker Transforms</returns>
        private Transform[] FindClosestTrackers(Transform body, Transform leftFoot, Transform rightFoot, GameObject[] ts)
        {
            Dictionary<Transform, (float, GameObject)?> distances = new Dictionary<Transform, (float, GameObject)?>
            {
                [body] = null,
                [leftFoot] = null,
                [rightFoot] = null
            };
            foreach (GameObject tracker in ts)
            {
                Vector3 p = tracker.transform.position;
                float bodyDistance = Vector3.Distance(body.position, p);
                float leftFootDistance = Vector3.Distance(leftFoot.position, p);
                float rightFootDistance = Vector3.Distance(rightFoot.position, p);
                if (distances[body] == null || bodyDistance < distances[body].Value.Item1)
                    distances[body] = (bodyDistance, tracker);
                if (distances[leftFoot] == null || leftFootDistance < distances[leftFoot].Value.Item1)
                    distances[leftFoot] = (leftFootDistance, tracker);
                if (distances[rightFoot] == null || rightFootDistance < distances[rightFoot].Value.Item1)
                    distances[rightFoot] = (rightFootDistance, tracker);
            }
            List<Transform> newTs = new();
            if(distances[body] == null)
                newTs.Add(null);
            else
                newTs.Add(distances[body].Value.Item2.transform.GetChild(0));
            if(distances[leftFoot] == null)
                newTs.Add(null);
            else
                newTs.Add(distances[leftFoot].Value.Item2.transform.GetChild(0));
            if(distances[rightFoot] == null)
                newTs.Add(null);
            else
                newTs.Add(distances[rightFoot].Value.Item2.transform.GetChild(0));
            return newTs.ToArray();
        }

        private bool a;

        internal void Update(bool areTwoTriggersClicked, Transform cameraTransform, Transform LeftHandReference, 
            Transform RightHandReference, bool isMoving)
        {
            if(MainAnimator != null && MainAnimator.isInitialized)
                MainAnimator.SetFloat("MotionSpeed", 1f);
            switch (calibrated)
            {
                case false:
                {
                    Transform t = headAlign.transform;
                    if (t == null)
                        break;
                    cameraTransform.position = t.position;
                    cameraTransform.rotation = t.rotation;
                    break;
                }
                case true:
                {
                    Transform t = LocalPlayer.Instance.Camera.transform;
                    cameraTransform.position = t.position;
                    cameraTransform.rotation = t.rotation;
                    break;
                }
            }
            if (vrik != null && vrik.solver.initiated && (!XRTracker.CanFBT || MainAnimator.avatar == null) && !calibrated)
            {
                VRIKCalibrator.Calibrate(vrik, vrikSettings, cameraTransform, null, LeftHandReference.transform,
                    RightHandReference.transform);
                SetupAnimators();
                calibrated = true;
            }
            else if (vrik != null && XRTracker.CanFBT && !calibrated)
            {
                if (areTwoTriggersClicked)
                {
                    GameObject[] ts = new GameObject[3];
                    int i = 0;
                    foreach (XRTracker tracker in XRTracker.Trackers)
                    {
                        if(tracker.TrackerRole == XRTrackerRole.Camera) continue;
                        ts[i] = tracker.gameObject;
                        i++;
                    }
                    if (ts[0] != null && ts[1] != null && ts[2] != null)
                    {
                        Transform body = GetBoneFromHumanoid(HumanBodyBones.Hips);
                        Transform leftFoot = GetBoneFromHumanoid(HumanBodyBones.LeftFoot);
                        Transform rightFoot = GetBoneFromHumanoid(HumanBodyBones.RightFoot);
                        if (body != null && leftFoot != null && rightFoot != null)
                        {
                            Transform[] newTs = FindClosestTrackers(body, leftFoot, rightFoot, ts);
                            if (newTs[0] != null && newTs[1] != null && newTs[2] != null)
                            {
                                newTs[0].rotation = body.rotation;
                                newTs[1].rotation = leftFoot.rotation;
                                newTs[2].rotation = rightFoot.rotation;
                                VRIKCalibrator.Calibrate(vrik, vrikSettings, cameraTransform, newTs[0],
                                    LeftHandReference.transform, RightHandReference.transform, newTs[1], newTs[2]);
                                RelaxWrists(GetBoneFromHumanoid(HumanBodyBones.LeftLowerArm),
                                    GetBoneFromHumanoid(HumanBodyBones.RightLowerArm), GetBoneFromHumanoid(HumanBodyBones.LeftHand),
                                    GetBoneFromHumanoid(HumanBodyBones.RightHand));
                                SetupAnimators();
                                calibrated = true;
                            }
                        }
                    }
                }
            }
            else if (vrik != null && calibrated)
            {
                vrik.solver.locomotion.weight = isMoving || XRTracker.CanFBT ? 0f : 1f;
                if (!XRTracker.CanFBT)
                {
                    float scale = LocalPlayer.Instance.transform.localScale.y;
                    float height = LocalPlayer.Instance.CharacterController.height;
                    vrik.solver.locomotion.footDistance = 0.1f * scale * height;
                    vrik.solver.locomotion.stepThreshold = 0.2f * scale * height;
                }
                MainAnimator.runtimeAnimatorController = Init.Instance.DefaultAvatarAnimatorController;
                // MotionSpeed (4)
                MainAnimator.SetFloat("MotionSpeed", 1f);
                MainAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            else if (vrik == null)
            {
                MainAnimator.runtimeAnimatorController = Init.Instance.DefaultAvatarAnimatorController;
                // MotionSpeed (4)
                MainAnimator.SetFloat("MotionSpeed", 1f);
                MainAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            if (!a)
            {
                if (vrik == null)
                    return;
                IKSolver s = vrik.GetIKSolver();
                if(s == null)
                    return;
                s.OnPostUpdate += () =>
                {
                    if (!LocalPlayer.IsVR)
                        return;
                    foreach (LocalPlayerSyncObject localPlayerSyncObject in new List<LocalPlayerSyncObject>(LocalPlayer.Instance.SavedTransforms))
                    {
                        if (localPlayerSyncObject == null)
                            LocalPlayer.Instance.SavedTransforms.Remove(localPlayerSyncObject);
                        else
                        {
                            localPlayerSyncObject.CheckTime = CheckTime.InvokeManually;
                            PathDescriptor pathDescriptor =
                                localPlayerSyncObject.gameObject.GetComponent<PathDescriptor>();
                            if(pathDescriptor == null || pathDescriptor.path == null) continue;
                            localPlayerSyncObject.Check();
                        }
                    }
                };
                a = true;
            }
            fingerCalibration?.Update();
        }

        internal void LateUpdate(bool isVR, Transform cameraTransform, bool lockCamera)
        {
            if (!isVR && headAlign != null && !lockCamera)
            {
                cameraTransform.position = headAlign.transform.position;
                Transform headBone = GetBoneFromHumanoid(HumanBodyBones.Head);
                if(headBone != null)
                    headBone.rotation = cameraTransform.rotation;
            }
            if (isVR)
            {
                // TODO: Properly Rotate Finger Bones on Avatars
                //fingerCalibration?.LateUpdate();
            }
            if (!isVR)
            {
                foreach (LocalPlayerSyncObject localPlayerSyncObject in new List<LocalPlayerSyncObject>(LocalPlayer.Instance.SavedTransforms))
                {
                    if (localPlayerSyncObject == null)
                        LocalPlayer.Instance.SavedTransforms.Remove(localPlayerSyncObject);
                    else
                    {
                        // Force Non-VR
                        localPlayerSyncObject.CheckTime = CheckTime.LateUpdate;
                    }
                }
            }
        }

        // Speed (0)
        internal void SetSpeed(float speed)
        {
            if (MainAnimator == null || !MainAnimator.isInitialized)
                return;
            MainAnimator.SetFloat("Speed", speed);
        }

        internal void SetIsGrounded(bool g)
        {
            if (MainAnimator == null || !MainAnimator.isInitialized)
                return;
            // Grounded (2)
            MainAnimator.SetBool("Grounded", g);
            // FreeFall (3)
            MainAnimator.SetBool("FreeFall", !g);
        }

        private Quaternion GetEyeQuaternion(float x, float y, Quaternion up, Quaternion down, Quaternion left,
            Quaternion right)
        {
            // what am i doing
            float xx = (left.x - right.x) / 2;
            float yy = (up.y - down.y) / 2;
            float zz = (left.z + right.z + up.z + down.z) / 4;
            Quaternion final = new Quaternion(xx * x, yy * y, zz, 0);
            return final;
        }

        internal void UpdateEyes(UnifiedEyeData eyeData)
        {
            if (!Avatar.UseEyeManager)
                return;
            if (Avatar.UseCombinedEyeBlendshapes)
            {
                float opennessValue = 1f - ((eyeData.Left.Openness + eyeData.Right.Openness) / 2);
                float leftValue = (eyeData.Left.Gaze.x >= 0f ? -eyeData.Left.Gaze.x :
                    0 + eyeData.Right.Gaze.x >= 0f ? -eyeData.Right.Gaze.x : 0) / 2;
                float rightValue = (eyeData.Left.Gaze.x >= 0f ? eyeData.Left.Gaze.x :
                    0 + eyeData.Right.Gaze.x >= 0f ? eyeData.Right.Gaze.x : 0) / 2;
                float downValue = (eyeData.Left.Gaze.y >= 0f ? -eyeData.Left.Gaze.y :
                    0 + eyeData.Right.Gaze.y >= 0f ? -eyeData.Right.Gaze.y : 0) / 2;
                float upValue = (eyeData.Left.Gaze.y >= 0f ? eyeData.Left.Gaze.y :
                    0 + eyeData.Right.Gaze.y >= 0f ? eyeData.Right.Gaze.y : 0) / 2;
                foreach (KeyValuePair<EyeBlendshapeAction,BlendshapeDescriptor> avatarEyeBlendshape in Avatar.EyeBlendshapes)
                {
                    switch (avatarEyeBlendshape.Key)
                    {
                        case EyeBlendshapeAction.Blink:
                            avatarEyeBlendshape.Value.SetWeight(opennessValue * 100);
                            break;
                        case EyeBlendshapeAction.LookUp:
                            avatarEyeBlendshape.Value.SetWeight(upValue * 100);
                            break;
                        case EyeBlendshapeAction.LookDown:
                            avatarEyeBlendshape.Value.SetWeight(downValue * 100);
                            break;
                        case EyeBlendshapeAction.LookRight:
                            avatarEyeBlendshape.Value.SetWeight(rightValue * 100);
                            break;
                        case EyeBlendshapeAction.LookLeft:
                            avatarEyeBlendshape.Value.SetWeight(leftValue * 100);
                            break;
                    }
                }
                SetParameter("LeftEyeBlink", opennessValue);
                SetParameter("LeftEyeLookLeft", leftValue);
                SetParameter("LeftEyeLookRight", rightValue);
                SetParameter("LeftEyeLookUp", upValue);
                SetParameter("LeftEyeLookDown", downValue);
                SetParameter("RightEyeBlink", opennessValue);
                SetParameter("RightEyeLookLeft", leftValue);
                SetParameter("RightEyeLookRight", rightValue);
                SetParameter("RightEyeLookUp", upValue);
                SetParameter("RightEyeLookDown", downValue);
            }
            else
            {
                // Left Eye
                float leftOpennessValue = 1f - eyeData.Left.Openness;
                float leftUpValue = eyeData.Left.Gaze.y > 0 ? eyeData.Left.Gaze.y : 0f;
                float leftDownValue = eyeData.Left.Gaze.y < 0 ? eyeData.Left.Gaze.y : 0f;
                float leftRightValue = eyeData.Left.Gaze.x > 0 ? eyeData.Left.Gaze.x : 0f;
                float leftLeftValue = eyeData.Left.Gaze.y < 0 ? eyeData.Left.Gaze.x : 0f;
                if (Avatar.UseLeftEyeBoneInstead)
                {
                    Avatar.LeftEyeBone.localRotation = GetEyeQuaternion(eyeData.Left.Gaze.x, eyeData.Left.Gaze.y,
                        Avatar.LeftEyeUpLimit, Avatar.LeftEyeDownLimit, Avatar.LeftEyeLeftLimit,
                        Avatar.LeftEyeRightLimit);
                }
                else
                {
                    foreach (KeyValuePair<EyeBlendshapeAction, BlendshapeDescriptor> avatarEyeBlendshape in Avatar
                                 .LeftEyeBlendshapes)
                    {
                        switch (avatarEyeBlendshape.Key)
                        {
                            case EyeBlendshapeAction.Blink:
                                avatarEyeBlendshape.Value.SetWeight(leftOpennessValue * 100);
                                break;
                            case EyeBlendshapeAction.LookUp:
                                avatarEyeBlendshape.Value.SetWeight(leftUpValue * 100);
                                break;
                            case EyeBlendshapeAction.LookDown:
                                avatarEyeBlendshape.Value.SetWeight(leftDownValue * 100);
                                break;
                            case EyeBlendshapeAction.LookRight:
                                avatarEyeBlendshape.Value.SetWeight(leftRightValue * 100);
                                break;
                            case EyeBlendshapeAction.LookLeft:
                                avatarEyeBlendshape.Value.SetWeight(leftLeftValue * 100);
                                break;
                        }
                    }
                }
                SetParameter("LeftEyeBlink", leftOpennessValue);
                SetParameter("LeftEyeLookLeft", leftLeftValue);
                SetParameter("LeftEyeLookRight", leftRightValue);
                SetParameter("LeftEyeLookUp", leftUpValue);
                SetParameter("LeftEyeLookDown", leftDownValue);
                // Right Eye
                float rightOpennessValue = 1f - eyeData.Right.Openness;
                float rightUpValue = eyeData.Right.Gaze.y > 0 ? eyeData.Right.Gaze.y : 0f;
                float rightDownValue = eyeData.Right.Gaze.y < 0 ? eyeData.Right.Gaze.y : 0f;
                float rightRightValue = eyeData.Right.Gaze.x > 0 ? eyeData.Right.Gaze.x : 0f;
                float rightLeftValue = eyeData.Right.Gaze.y < 0 ? eyeData.Right.Gaze.x : 0f;
                if (Avatar.UseRightEyeBoneInstead)
                {
                    Avatar.RightEyeBone.localRotation = GetEyeQuaternion(eyeData.Right.Gaze.x, eyeData.Right.Gaze.y,
                        Avatar.RightEyeUpLimit, Avatar.RightEyeDownLimit, Avatar.RightEyeLeftLimit,
                        Avatar.RightEyeRightLimit);
                }
                else
                {
                    foreach (KeyValuePair<EyeBlendshapeAction, BlendshapeDescriptor> avatarEyeBlendshape in Avatar
                                 .RightEyeBlendshapes)
                    {
                        switch (avatarEyeBlendshape.Key)
                        {
                            case EyeBlendshapeAction.Blink:
                                avatarEyeBlendshape.Value.SetWeight(rightOpennessValue * 100);
                                break;
                            case EyeBlendshapeAction.LookUp:
                                avatarEyeBlendshape.Value.SetWeight(rightUpValue * 100);
                                break;
                            case EyeBlendshapeAction.LookDown:
                                avatarEyeBlendshape.Value.SetWeight(rightDownValue * 100);
                                break;
                            case EyeBlendshapeAction.LookRight:
                                avatarEyeBlendshape.Value.SetWeight(rightRightValue * 100);
                                break;
                            case EyeBlendshapeAction.LookLeft:
                                avatarEyeBlendshape.Value.SetWeight(rightLeftValue * 100);
                                break;
                        }
                    }
                }
                SetParameter("RightEyeBlink", rightOpennessValue);
                SetParameter("RightEyeLookLeft", rightLeftValue);
                SetParameter("RightEyeLookRight", rightRightValue);
                SetParameter("RightEyeLookUp", rightUpValue);
                SetParameter("RightEyeLookDown", rightDownValue);
            }
            if(FaceTrackingDescriptor != null)
                foreach (KeyValuePair<ExtraEyeExpressions,BlendshapeDescriptors> extraEyeValue in FaceTrackingDescriptor.ExtraEyeValues)
                {
                    switch (extraEyeValue.Key)
                    {
                        case ExtraEyeExpressions.PupilDilation:
                        {
                            float v = (eyeData.Left.PupilDiameter_MM + eyeData.Right.PupilDiameter_MM) / 2;
                            foreach (BlendshapeDescriptor blendshapeDescriptor in extraEyeValue.Value.Descriptors)
                            {
                                if (blendshapeDescriptor == null || blendshapeDescriptor.SkinnedMeshRenderer == null)
                                    continue;
                                SetBlendshapeWeight(blendshapeDescriptor.SkinnedMeshRenderer,
                                    blendshapeDescriptor.BlendshapeIndex, v);
                            }
                            SetParameter("PupilDilation", v);
                            break;
                        }
                    }
                }
        }

        internal void UpdateFace(Dictionary<FaceExpressions, float> weights)
        {
            if (FaceTrackingDescriptor == null)
            {
                foreach (FaceExpressions faceExpression in weights.Keys)
                    SetParameter(faceExpression.ToString(), 0);
                return;
            }
            foreach (KeyValuePair<FaceExpressions,float> keyValuePair in weights)
            {
                if (!FaceTrackingDescriptor.FaceValues.ContainsKey(keyValuePair.Key)) continue;
                BlendshapeDescriptor blendshapeDescriptor = FaceTrackingDescriptor.FaceValues[keyValuePair.Key];
                if (blendshapeDescriptor != null && blendshapeDescriptor.SkinnedMeshRenderer != null)
                {
                    SetBlendshapeWeight(blendshapeDescriptor.SkinnedMeshRenderer,
                        blendshapeDescriptor.BlendshapeIndex, keyValuePair.Value * 100);
                    SetParameter(keyValuePair.Key.ToString(), keyValuePair.Value);
                }
                else
                    SetParameter(keyValuePair.Key.ToString(), 0);
            }
        }

        public void Dispose()
        {
            foreach (AnimatorPlayable playableAnimator in PlayableAnimators)
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
            foreach (string s in new List<string>(LocalAvatarLocalAvatar.AssignedTags))
            {
                foreach (string morePlayerAssignedTag in new List<string>(LocalPlayer.MorePlayerAssignedTags))
                {
                    if (s == morePlayerAssignedTag)
                        LocalPlayer.MorePlayerAssignedTags.Remove(morePlayerAssignedTag);
                }
            }
            foreach (string s in new List<string>(LocalAvatarLocalAvatar.ExtraneousKeys))
            {
                foreach (KeyValuePair<string, object> extraneousObject in new Dictionary<string, object>(LocalPlayer
                             .MoreExtraneousObjects))
                    if (s == extraneousObject.Key)
                        LocalPlayer.MoreExtraneousObjects.Remove(extraneousObject.Key);
            }
            Object.Destroy(Avatar.gameObject);
        }
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