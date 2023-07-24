using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game.Bindings;
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
using UnityEngine.XR;
using VRCFaceTracking.Core.Params.Data;
using Avatar = Hypernex.CCK.Unity.Avatar;
using Object = UnityEngine.Object;

namespace Hypernex.Game
{
    public class AvatarCreator : IDisposable
    {
        public Avatar Avatar;
        public Animator MainAnimator;
        public FaceTrackingDescriptor FaceTrackingDescriptor;
        public List<AnimatorPlayable> AnimatorPlayables => new (PlayableAnimators);

        private List<AnimatorPlayable> PlayableAnimators = new ();
        internal List<Sandbox> localAvatarSandboxes = new();
        private VRIK vrik;
        private bool calibrated;
        private VRIKCalibrator.Settings vrikSettings = new()
        {
            scaleMlp = 1f,
            handOffset = new Vector3(0, 0.01f, -0.1f),
        };
        private GameObject headAlign;
        internal Transform nametagAlign;
        internal GameObject voiceAlign;
        internal AudioSource audioSource;
        internal OpusHandler opusHandler;
        private List<AvatarNearClip> avatarNearClips = new();
        private OVRLipSyncContext lipSyncContext;
        private List<OVRLipSyncContextMorphTarget> morphTargets = new ();

        public AvatarCreator(LocalPlayer localPlayer, Avatar a, bool isVR)
        {
            a = Object.Instantiate(a.gameObject).GetComponent<Avatar>();
            a.gameObject.AddComponent<AvatarBehaviour>();
            Avatar = a;
            SceneManager.MoveGameObjectToScene(a.gameObject, localPlayer.gameObject.scene);
            MainAnimator = a.GetComponent<Animator>();
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
                if (XRTracker.Trackers.Count(x => x.IsTracked) != 3)
                {
                    RelaxWrists(GetBoneFromHumanoid(HumanBodyBones.LeftLowerArm),
                        GetBoneFromHumanoid(HumanBodyBones.RightLowerArm), GetBoneFromHumanoid(HumanBodyBones.LeftHand),
                        GetBoneFromHumanoid(HumanBodyBones.RightHand));
                }
            }
            else
                SetupAnimators();
            calibrated = false;
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in Avatar.gameObject
                         .GetComponentsInChildren<SkinnedMeshRenderer>())
                skinnedMeshRenderer.updateWhenOffscreen = true;
            if (string.IsNullOrEmpty(LocalPlayer.Instance.avatarMeta.ImageURL))
                CurrentAvatarBanner.Instance.Render(this, Array.Empty<byte>());
            else
                DownloadTools.DownloadBytes(LocalPlayer.Instance.avatarMeta.ImageURL,
                    bytes => CurrentAvatarBanner.Instance.Render(this, bytes));
            SetupLipSyncLocalPlayer();
            InitMaterialDescriptors(a.transform);
        }

        public AvatarCreator(NetPlayer netPlayer, Avatar a)
        {
            a = Object.Instantiate(a.gameObject).GetComponent<Avatar>();
            Avatar = a;
            SceneManager.MoveGameObjectToScene(a.gameObject, netPlayer.gameObject.scene);
            MainAnimator = a.GetComponent<Animator>();
            MainAnimator.runtimeAnimatorController = null;
            voiceAlign = new GameObject("voicealign_" + Guid.NewGuid());
            voiceAlign.transform.SetParent(a.SpeechPosition.transform);
            voiceAlign.transform.SetLocalPositionAndRotation(Vector3.zero, new Quaternion(0,0,0,0));
            voiceAlign.AddComponent<AudioSource>();
            opusHandler = voiceAlign.AddComponent<OpusHandler>();
            opusHandler.OnDecoded += opusHandler.PlayDecodedToVoice;
            audioSource = opusHandler.gameObject.GetComponent<AudioSource>();
            audioSource.spatialize = true;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 0;
            audioSource.maxDistance = 10;
            audioSource.outputAudioMixerGroup = Init.Instance.VoiceGroup;
            nametagAlign = new GameObject("nametagalign_" + Guid.NewGuid()).transform;
            Transform head = GetBoneFromHumanoid(HumanBodyBones.Head);
            if (head != null)
            {
                nametagAlign.transform.parent = head;
                nametagAlign.transform.localPosition = new Vector3(0,
                    head.localPosition.y + 1.6f, 0);
                nametagAlign.transform.SetParent(netPlayer.transform, false);
                netPlayer.nameplateTemplate.FollowTransform = nametagAlign.transform;
            }
            else
                Object.Destroy(nametagAlign.gameObject);
            a.transform.SetParent(netPlayer.transform);
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
            SetupLipSyncNetPlayer();
            InitMaterialDescriptors(a.transform);
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

        internal Dictionary<string, float> GetAnimatorWeights()
        {
            Dictionary<string, float> weights = new Dictionary<string, float>();
            foreach (AnimatorPlayable playableAnimator in PlayableAnimators)
            {
                foreach (AnimatorControllerParameter playableAnimatorControllerParameter in playableAnimator.AnimatorControllerParameters)
                {
                    if (!weights.ContainsKey(playableAnimatorControllerParameter.name))
                        switch (playableAnimatorControllerParameter.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                weights.Add(playableAnimatorControllerParameter.name,
                                    playableAnimator.AnimatorControllerPlayable.GetBool(
                                        playableAnimatorControllerParameter.name)
                                        ? 1.00f
                                        : 0.00f);
                                break;
                            case AnimatorControllerParameterType.Int:
                                weights.Add(playableAnimatorControllerParameter.name,
                                    playableAnimator.AnimatorControllerPlayable.GetInteger(
                                        playableAnimatorControllerParameter.name));
                                break;
                            case AnimatorControllerParameterType.Float:
                                weights.Add(playableAnimatorControllerParameter.name,
                                    playableAnimator.AnimatorControllerPlayable.GetFloat(
                                        playableAnimatorControllerParameter.name));
                                break;
                        }
                }
            }
            return weights;
        }

        internal void HandleNetParameter(string parameterName, float weight)
        {
            foreach (AnimatorPlayable playableAnimator in PlayableAnimators)
            {
                AnimatorControllerParameter parameter = GetParameterByName(parameterName, playableAnimator);
                if (parameter != null)
                {
                    switch (parameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            playableAnimator.AnimatorControllerPlayable.SetBool(parameterName,
                                Math.Abs(weight - 1.00f) < 0.01);
                            break;
                        case AnimatorControllerParameterType.Int:
                            playableAnimator.AnimatorControllerPlayable.SetInteger(parameterName, (int) weight);
                            break;
                        case AnimatorControllerParameterType.Float:
                            playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName, weight);
                            break;
                    }
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
            if (vrik != null && vrik.solver.initiated && XRTracker.Trackers.Count(x => x.IsTracked) != 3 && !calibrated)
            {
                VRIKCalibrator.Calibrate(vrik, vrikSettings, cameraTransform, null, LeftHandReference.transform,
                    RightHandReference.transform);
                SetupAnimators();
                calibrated = true;
            }
            else if (vrik != null && XRTracker.Trackers.Count(x => x.IsTracked) == 3 && !calibrated)
            {
                XRTracker.Trackers.ForEach(x =>
                {
                    Renderer r = x.renderer;
                    if (r != null)
                        r.enabled = true;
                });
                if (areTwoTriggersClicked)
                {
                    GameObject[] ts = new GameObject[3];
                    int i = 0;
                    foreach (XRTracker tracker in XRTracker.Trackers)
                    {
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
                                //vrik = Avatar.gameObject.AddComponent<VRIK>();
                                VRIKCalibrator.Calibrate(vrik, vrikSettings, cameraTransform, newTs[0],
                                    LeftHandReference.transform, RightHandReference.transform, newTs[1], newTs[2]);
                                RelaxWrists(GetBoneFromHumanoid(HumanBodyBones.LeftLowerArm),
                                    GetBoneFromHumanoid(HumanBodyBones.RightLowerArm), GetBoneFromHumanoid(HumanBodyBones.LeftHand),
                                    GetBoneFromHumanoid(HumanBodyBones.RightHand));
                                SetupAnimators();
                                calibrated = true;
                                XRTracker.Trackers.ForEach(x =>
                                {
                                    Renderer r = x.renderer;
                                    if (r != null)
                                        r.enabled = false;
                                });
                            }
                        }
                    }
                }
            }
            else if (calibrated)
            {
                vrik.solver.locomotion.weight = isMoving || XRTracker.Trackers.Count(x => x.IsTracked) == 3 ? 0f : 1f;
                if (XRTracker.Trackers.Count(x => x.IsTracked) != 3)
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
                    foreach (PathDescriptor pathDescriptor in new List<PathDescriptor>(LocalPlayer.Instance.SavedTransforms))
                    {
                        if (pathDescriptor == null)
                            LocalPlayer.Instance.SavedTransforms.Remove(pathDescriptor);
                        else
                        {
                            if(pathDescriptor.path == null) continue;
                            NetworkedObject networkedObject = new NetworkedObject
                            {
                                ObjectLocation = pathDescriptor.path,
                                Position = NetworkConversionTools.Vector3Tofloat3(
                                    pathDescriptor.transform.localPosition),
                                Rotation = new float4(pathDescriptor.transform.localEulerAngles.x,
                                    pathDescriptor.transform.localEulerAngles.y,
                                    pathDescriptor.transform.localEulerAngles.z, 0),
                                Size = NetworkConversionTools.Vector3Tofloat3(pathDescriptor.transform.localScale)
                            };
                            if (!LocalPlayer.Instance.children.ContainsKey(pathDescriptor.path))
                                LocalPlayer.Instance.children.Add(pathDescriptor.path, networkedObject);
                            else
                                LocalPlayer.Instance.children[pathDescriptor.path] = networkedObject;
                        }
                    }
                };
                a = true;
            }
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
            if (!isVR)
            {
                foreach (PathDescriptor pathDescriptor in new List<PathDescriptor>(LocalPlayer.Instance.SavedTransforms))
                {
                    if (pathDescriptor == null)
                        LocalPlayer.Instance.SavedTransforms.Remove(pathDescriptor);
                    else
                    {
                        if(pathDescriptor.path == null) continue;
                        NetworkedObject networkedObject = new NetworkedObject
                        {
                            ObjectLocation = pathDescriptor.path,
                            Position = NetworkConversionTools.Vector3Tofloat3(
                                pathDescriptor.transform.localPosition),
                            Rotation = new float4(pathDescriptor.transform.localEulerAngles.x,
                                pathDescriptor.transform.localEulerAngles.y,
                                pathDescriptor.transform.localEulerAngles.z, 0),
                            Size = NetworkConversionTools.Vector3Tofloat3(pathDescriptor.transform.localScale)
                        };
                        if (!LocalPlayer.Instance.children.ContainsKey(pathDescriptor.path))
                            LocalPlayer.Instance.children.Add(pathDescriptor.path, networkedObject);
                        else
                            LocalPlayer.Instance.children[pathDescriptor.path] = networkedObject;
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
            Quaternion final = new Quaternion((right.x - left.x)*x, (up.y - down.y)*y, (up*down).z/2, 0);
            return final;
        }

        internal void UpdateEyes(UnifiedEyeData eyeData)
        {
            if (!Avatar.UseEyeManager)
                return;
            // Left Eye
            if (Avatar.UseLeftEyeBoneInstead)
            {
                Avatar.LeftEyeBone.localRotation = GetEyeQuaternion(eyeData.Left.Gaze.x, eyeData.Left.Gaze.y,
                    Avatar.LeftEyeUpLimit, Avatar.LeftEyeDownLimit, Avatar.LeftEyeLeftLimit, Avatar.LeftEyeRightLimit);
            }
            else
            {
                foreach (KeyValuePair<EyeBlendshapeAction,BlendshapeDescriptor> avatarEyeBlendshape in Avatar.EyeBlendshapes)
                {
                    switch (avatarEyeBlendshape.Key)
                    {
                        case EyeBlendshapeAction.Blink:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Left.Openness * 100);
                            break;
                        case EyeBlendshapeAction.LookUp:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Left.Gaze.y > 0 ? eyeData.Left.Gaze.y * 100 : 0f);
                            break;
                        case EyeBlendshapeAction.LookDown:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Left.Gaze.y < 0 ? eyeData.Left.Gaze.y * 100 : 0f);
                            break;
                        case EyeBlendshapeAction.LookRight:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Left.Gaze.x > 0 ? eyeData.Left.Gaze.x * 100 : 0f);
                            break;
                        case EyeBlendshapeAction.LookLeft:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Left.Gaze.y < 0 ? eyeData.Left.Gaze.x * 100 : 0f);
                            break;
                    }
                }
            }
            // Right Eye
            if (Avatar.UseRightEyeBoneInstead)
            {
                Avatar.RightEyeBone.localRotation = GetEyeQuaternion(eyeData.Right.Gaze.x, eyeData.Right.Gaze.y,
                    Avatar.RightEyeUpLimit, Avatar.RightEyeDownLimit, Avatar.RightEyeLeftLimit,
                    Avatar.RightEyeRightLimit);
            }
            else
            {
                foreach (KeyValuePair<EyeBlendshapeAction,BlendshapeDescriptor> avatarEyeBlendshape in Avatar.RightEyeBlendshapes)
                {
                    switch (avatarEyeBlendshape.Key)
                    {
                        case EyeBlendshapeAction.Blink:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Right.Openness * 100);
                            break;
                        case EyeBlendshapeAction.LookUp:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Right.Gaze.y > 0 ? eyeData.Right.Gaze.y * 100 : 0f);
                            break;
                        case EyeBlendshapeAction.LookDown:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Right.Gaze.y < 0 ? eyeData.Right.Gaze.y * 100 : 0f);
                            break;
                        case EyeBlendshapeAction.LookRight:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Right.Gaze.x > 0 ? eyeData.Right.Gaze.x * 100 : 0f);
                            break;
                        case EyeBlendshapeAction.LookLeft:
                            avatarEyeBlendshape.Value.SetWeight(eyeData.Right.Gaze.y < 0 ? eyeData.Right.Gaze.x * 100 : 0f);
                            break;
                    }
                }
            }
        }

        internal void UpdateFace(Dictionary<FaceExpressions, float> weights)
        {
            if (FaceTrackingDescriptor == null)
                return;
            foreach (KeyValuePair<FaceExpressions,float> keyValuePair in weights)
            {
                if (!FaceTrackingDescriptor.FaceValues.ContainsKey(keyValuePair.Key)) continue;
                BlendshapeDescriptor blendshapeDescriptor = FaceTrackingDescriptor.FaceValues[keyValuePair.Key];
                if (blendshapeDescriptor != null)
                    SetBlendshapeWeight(blendshapeDescriptor.SkinnedMeshRenderer,
                        blendshapeDescriptor.BlendshapeIndex, keyValuePair.Value);
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
            if(opusHandler != null)
                opusHandler.OnDecoded -= opusHandler.PlayDecodedToVoice;
            if(nametagAlign != null)
            {
                Object.Destroy(nametagAlign.gameObject);
                nametagAlign = null;
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