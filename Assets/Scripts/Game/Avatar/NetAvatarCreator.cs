using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hypernex.Game.Avatar
{
    public class NetAvatarCreator : AvatarCreator
    {
        private NetPlayer netPlayer;

        private Dictionary<string, Transform> cachedTransforms = new();
        private Dictionary<Transform, SkinnedMeshRenderer> cachedSkinnedMeshRenderers = new();

        public NetAvatarCreator(NetPlayer np, CCK.Unity.Avatar a, bool isVR)
        {
            netPlayer = np;
            a = Object.Instantiate(a.gameObject).GetComponent<CCK.Unity.Avatar>();
            Avatar = a;
            SceneManager.MoveGameObjectToScene(a.gameObject, np.gameObject.scene);
            MainAnimator = a.GetComponent<Animator>();
            OnCreate(Avatar, 10);
            VoiceAlign = new GameObject("voicealign_" + Guid.NewGuid());
            VoiceAlign.transform.SetParent(a.SpeechPosition.transform);
            VoiceAlign.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            audioSource = VoiceAlign.AddComponent<AudioSource>();
            audioSource.spatialize = true;
            audioSource.spatializePostEffects = true;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 0;
            audioSource.maxDistance = 10;
            audioSource.outputAudioMixerGroup = Init.Instance.VoiceGroup;
            if(np.nameplateTemplate != null)
                np.nameplateTemplate.OnNewAvatar(this);
            a.transform.SetParent(np.transform);
            a.gameObject.name = "avatar";
            SetAvatarPosition(isVR);
            if (isVR)
                StartVRIK(np);
            else
            {
                SetupAnimators();
                Calibrated = true;
            }
            SetupLipSyncNetPlayer();
        }

        private void SetAvatarPosition(bool isVR) => Avatar.transform.SetLocalPositionAndRotation(
            isVR ? new Vector3(0, 0, 0) : new Vector3(0, -(Avatar.transform.localScale.y * 0.75f), 0),
            new Quaternion(0, 0, 0, 0));
        
        private void SetupLipSyncNetPlayer()
        {
            if (!Avatar.UseVisemes) return;
            lipSyncContext = VoiceAlign.AddComponent<OVRLipSyncContext>();
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
        
        internal void HandleNetParameter(WeightedObjectUpdate weight)
        {
            switch (weight.TypeOfWeight.ToLower())
            {
                case PARAMETER_ID:
                {
                    string parameterName = weight.WeightIndex;
                    if (weight.PathToWeightContainer == MAIN_ANIMATOR)
                    {
                        if(MainAnimator == null || MainAnimator.runtimeAnimatorController == null) break;
                        try
                        {
                            AnimatorControllerParameter parameter =
                                MainAnimator.parameters.First(x => x.name == weight.WeightIndex);
                            switch (parameter.type)
                            {
                                case AnimatorControllerParameterType.Bool:
                                    MainAnimator.SetBool(parameter.name, Math.Abs(weight.Weight - 1.00f) < 0.01);
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    MainAnimator.SetInteger(parameter.name, (int) weight.Weight);
                                    break;
                                case AnimatorControllerParameterType.Float:
                                    MainAnimator.SetFloat(parameter.name, weight.Weight);
                                    break;
                            }
                        } catch(Exception){}
                        break;
                    }
                    foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
                    {
                        if (playableAnimator.CustomPlayableAnimator.AnimatorController.name !=
                            weight.PathToWeightContainer && weight.PathToWeightContainer != ALL_ANIMATOR_LAYERS) continue;
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
                        if(!cachedTransforms.TryGetValue(weight.PathToWeightContainer, out Transform t))
                        {
                            t = Avatar.transform.parent.Find(weight.PathToWeightContainer);
                            if(t == null) break;
                            cachedTransforms.Add(weight.PathToWeightContainer, t);
                        }
                        if(!cachedSkinnedMeshRenderers.TryGetValue(t, out SkinnedMeshRenderer s))
                        {
                            s = t.gameObject.GetComponent<SkinnedMeshRenderer>();
                            if (s == null) break;
                            cachedSkinnedMeshRenderers.Add(t, s);
                        }
                        s.SetBlendShapeWeight(Convert.ToInt32(weight.WeightIndex), weight.Weight);
                    } catch(Exception){}
                    break;
                }
            }
        }

        internal void DestroyIK(bool vr)
        {
            VRIKRootController rootController = Avatar.GetComponent<VRIKRootController>();
            if(rootController != null)
                Object.Destroy(rootController);
            if(vrik != null)
                Object.Destroy(vrik);
            foreach (TwistRelaxer twistRelaxer in Avatar.gameObject.GetComponentsInChildren<TwistRelaxer>())
                Object.Destroy(twistRelaxer);
            SetAvatarPosition(false);
            if(vr)
                Calibrated = false;
        }

        private void StartVRIK(NetPlayer np)
        {
            SetAvatarPosition(true);
            vrik = Avatar.gameObject.AddComponent<VRIK>();
            Transform headReference = np.GetReferenceFromCoreBone(CoreBone.Head);
            Transform leftHandReference = np.GetReferenceFromCoreBone(CoreBone.LeftHand);
            Transform rightHandReference = np.GetReferenceFromCoreBone(CoreBone.RightHand);
            for (int i = 0; i < headReference.childCount; i++)
            {
                Transform child = headReference.GetChild(i);
                if (child.name == "Head Target")
                    Object.Destroy(child.gameObject);
            }
            for (int i = 0; i < leftHandReference.childCount; i++)
            {
                Transform child = leftHandReference.GetChild(i);
                Object.Destroy(child.gameObject);
            }
            for (int i = 0; i < rightHandReference.childCount; i++)
            {
                Transform child = rightHandReference.GetChild(i);
                Object.Destroy(child.gameObject);
            }
        }

        internal void CalibrateVRIK(bool fbt, VRIKCalibrator.CalibrationData calibrationData)
        {
            if (vrik == null)
                StartVRIK(netPlayer);
            if(vrik.solver == null || !vrik.solver.initiated)
                return;
            RelaxWrists(GetBoneFromHumanoid(HumanBodyBones.LeftLowerArm),
                GetBoneFromHumanoid(HumanBodyBones.RightLowerArm), GetBoneFromHumanoid(HumanBodyBones.LeftHand),
                GetBoneFromHumanoid(HumanBodyBones.RightHand));
            Transform headReference = netPlayer.GetReferenceFromCoreBone(CoreBone.Head);
            Transform leftHandReference = netPlayer.GetReferenceFromCoreBone(CoreBone.LeftHand);
            Transform rightHandReference = netPlayer.GetReferenceFromCoreBone(CoreBone.RightHand);
            if (!fbt)
            {
                VRIKCalibrator.Calibrate(vrik, calibrationData, headReference, null, leftHandReference,
                    rightHandReference);
                vrik.solver.locomotion.stepThreshold = 0.01f;
                vrik.solver.locomotion.angleThreshold = 20;
                vrik.solver.plantFeet = false;
                SetupAnimators();
                Calibrated = true;
                return;
            }
            Transform body = netPlayer.GetReferenceFromCoreBone(CoreBone.Hip);
            Transform leftFoot = netPlayer.GetReferenceFromCoreBone(CoreBone.LeftFoot);
            Transform rightFoot = netPlayer.GetReferenceFromCoreBone(CoreBone.RightFoot);
            if (body != null && leftFoot != null && rightFoot != null)
            {
                VRIKCalibrator.Calibrate(vrik, calibrationData, headReference, body, leftHandReference,
                    rightHandReference, leftFoot, rightFoot);
                SetupAnimators();
                Calibrated = true;
            }
        }

        internal void Update(bool fbt)
        {
            // TODO: This shouldn't be the solution. If an Animator isn't available, this just won't work.
            bool isMoving = MainAnimator.GetFloat("MoveX") != 0 || MainAnimator.GetFloat("MoveY") != 0;
            if(MainAnimator != null && MainAnimator.isInitialized)
                MainAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (vrik != null && Calibrated)
            {
                if(fbt)
                {
                    vrik.solver.spine.pelvisPositionWeight = 1f;
                    vrik.solver.spine.pelvisRotationWeight = 1f;
                }
                else
                {
                    float scale = LocalPlayer.Instance.transform.localScale.y;
                    float height = LocalPlayer.Instance.CharacterController.height;
                    vrik.solver.locomotion.footDistance = 0.1f * scale * height;
                    vrik.solver.locomotion.stepThreshold = 0.2f * scale * height;
                    vrik.solver.spine.pelvisPositionWeight = 0;
                    vrik.solver.spine.pelvisRotationWeight = 0;
                }
                vrik.solver.locomotion.weight = isMoving || fbt ? 0f : 1f;
                if(MainAnimator.runtimeAnimatorController == null)
                    MainAnimator.runtimeAnimatorController = animatorController;
            }
            else if (vrik != null && !Calibrated)
                MainAnimator.runtimeAnimatorController = null;
            else if(vrik == null)
                MainAnimator.runtimeAnimatorController = animatorController;
        }

        internal void LateUpdate(Transform referenceHead)
        {
            if(MainAnimator.GetBool("Crawling")) return;
            DriveCamera(referenceHead);
        }

        public override void Dispose()
        {
            cachedTransforms.Clear();
            cachedSkinnedMeshRenderers.Clear();
            base.Dispose();
        }
    }
}