using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Sandboxing;
using Hypernex.Sandboxing.SandboxedTypes;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Avatar = Hypernex.CCK.Unity.Avatar;
using Object = UnityEngine.Object;

namespace Hypernex.Game
{
    public class AvatarCreator : IDisposable
    {
        public Avatar Avatar;
        public Animator MainAnimator;
        public FaceTrackingDescriptor FaceTrackingDescriptor;
        
        private List<AnimatorPlayable> PlayableAnimators = new ();
        private List<Sandbox> localAvatarSandboxes = new();
        private VRIK vrik;
        private bool isCalibrating;
        private bool calibrated;
        private VRIKCalibrator.CalibrationData calibrationData = new();
        private GameObject headAlign;
        
        public AvatarCreator(LocalPlayer localPlayer, Avatar a, bool isVR)
        {
            a = Object.Instantiate(a.gameObject).GetComponent<Avatar>();
            a.gameObject.AddComponent<AvatarBehaviour>();
            Avatar = a;
            SceneManager.MoveGameObjectToScene(a.gameObject, localPlayer.gameObject.scene);
            MainAnimator = a.GetComponent<Animator>();
            headAlign = new GameObject("headalign_" + new Guid().ToString());
            headAlign.transform.position = a.ViewPosition;
            headAlign.transform.SetParent(GetBoneFromHumanoid(HumanBodyBones.Head), true);
            a.transform.SetParent(localPlayer.transform);
            a.transform.SetLocalPositionAndRotation(new Vector3(0, -1, 0), new Quaternion(0, 0, 0, 0));
            MainAnimator.runtimeAnimatorController = Init.Instance.DefaultAvatarAnimatorController;
            // MotionSpeed (4)
            MainAnimator.SetFloat("MotionSpeed", 1f);
            foreach (CustomPlayableAnimator customPlayableAnimator in a.Animators)
            {
                if (customPlayableAnimator == null) continue;
                if (customPlayableAnimator.AnimatorOverrideController != null)
                    customPlayableAnimator.AnimatorOverrideController.runtimeAnimatorController =
                        customPlayableAnimator.AnimatorController;
                PlayableGraph playableGraph = PlayableGraph.Create();
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
            if (isVR)
                vrik = Avatar.gameObject.AddComponent<VRIK>();
            isCalibrating = false;
            calibrated = false;
            foreach (NexboxScript localAvatarScript in a.LocalAvatarScripts)
                localAvatarSandboxes.Add(new Sandbox(localAvatarScript, SandboxRestriction.LocalAvatar));
        }

        public AvatarCreator(NetPlayer netPlayer, Avatar a)
        {
            a = Object.Instantiate(a.gameObject).GetComponent<Avatar>();
            Avatar = a;
            SceneManager.MoveGameObjectToScene(a.gameObject, netPlayer.gameObject.scene);
            a.transform.SetParent(netPlayer.transform);
            a.transform.SetLocalPositionAndRotation(new Vector3(0, -1, 0), new Quaternion(0, 0, 0, 0));
            MainAnimator = a.GetComponent<Animator>();
            foreach (CustomPlayableAnimator customPlayableAnimator in a.Animators)
            {
                if (customPlayableAnimator.AnimatorOverrideController != null)
                    customPlayableAnimator.AnimatorOverrideController.runtimeAnimatorController =
                        customPlayableAnimator.AnimatorController;
                PlayableGraph playableGraph = PlayableGraph.Create();
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
            foreach (NexboxScript localAvatarScript in a.LocalAvatarScripts)
                localAvatarSandboxes.Add(new Sandbox(localAvatarScript, SandboxRestriction.LocalAvatar));
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
                                    animatorPlayable.Value.AnimatorControllerPlayable.GetInteger(parameterName), typeof(T));
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
                                    playableAnimator.AnimatorControllerPlayable.GetInteger(parameterName), typeof(T));
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

        public void SetParameter<T>(string parameterName, T value)
        {
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
        
        /// <summary>
        /// Sorts Trackers from 0 by how close they are to the Body, LeftFoot, and RightFoot
        /// </summary>
        /// <returns>Sorted Tracker Transforms</returns>
        private Transform[] FindClosestTrackers(Transform body, Transform leftFoot, Transform rightFoot, GameObject[] ts)
        {
            List<Transform> newTs = new();
            foreach (GameObject o in ts)
            {
                float bodyDist = Vector3.Distance(body.position, o.transform.position);
                float leftFootDist = Vector3.Distance(leftFoot.position, o.transform.position);
                float rightFootDist = Vector3.Distance(rightFoot.position, o.transform.position);
                Transform target;
                if (leftFootDist < bodyDist)
                    target = leftFoot;
                else if (rightFootDist < bodyDist)
                    target = rightFoot;
                else
                    target = body;
                if(!newTs.Contains(target))
                    newTs.Add(target);
            }
            return newTs.ToArray();
        }

        internal void Update(bool areTwoTriggersClicked, Dictionary<InputDevice, GameObject> WorldTrackers,
            Transform cameraTransform, Transform LeftHandReference, Transform RightHandReference)
        {
            if (headAlign != null)
                cameraTransform.position = headAlign.transform.position;
            if(MainAnimator != null)
                MainAnimator.SetFloat("MotionSpeed", 1f);
            if (!calibrated && WorldTrackers.Count == 3 && vrik != null)
                isCalibrating = true;
            else if (WorldTrackers.Count != 3 && vrik != null)
            {
                VRIKCalibrator.Calibrate(vrik, calibrationData, cameraTransform, null, LeftHandReference.transform,
                    RightHandReference.transform);
                isCalibrating = false;
                calibrated = true;
            }
            if (isCalibrating && areTwoTriggersClicked && vrik != null)
            {
                GameObject[] ts = new GameObject[3];
                int i = 0;
                foreach (KeyValuePair<InputDevice, GameObject> keyValuePair in WorldTrackers)
                    ts[i] = keyValuePair.Value;
                if (ts[0] != null && ts[1] != null && ts[2] != null)
                {
                    Transform body = GetBoneFromHumanoid(HumanBodyBones.Hips);
                    Transform leftFoot = GetBoneFromHumanoid(HumanBodyBones.LeftFoot);
                    Transform rightFoot = GetBoneFromHumanoid(HumanBodyBones.RightFoot);
                    if (body != null && leftFoot != null && rightFoot != null)
                    {
                        Transform[] newTs = FindClosestTrackers(body, leftFoot, rightFoot, ts);
                        VRIKCalibrator.Calibrate(vrik, calibrationData, cameraTransform, newTs[0].transform,
                            LeftHandReference.transform, RightHandReference.transform, newTs[1], newTs[2]);
                        calibrated = true;
                        isCalibrating = false;
                    }
                }
            }
        }

        // Speed (0)
        internal void SetSpeed(float speed) => MainAnimator.SetFloat("Speed", speed);

        internal void SetIsGrounded(bool g)
        {
            // Grounded (2)
            MainAnimator.SetBool("Grounded", g);
            // FreeFall (3)
            MainAnimator.SetBool("FreeFall", !g);
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
                playableAnimator.PlayableGraph.Destroy();
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