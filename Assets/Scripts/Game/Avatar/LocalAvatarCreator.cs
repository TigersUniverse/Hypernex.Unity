using System;
using System.Collections.Generic;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game.Audio;
using Hypernex.Game.Bindings;
using Hypernex.Game.Networking;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRCFaceTracking.Core.Params.Data;
using Object = UnityEngine.Object;

namespace Hypernex.Game.Avatar
{
    public class LocalAvatarCreator : AvatarCreator
    {
        private VRIKCalibrator.Settings vrikSettings = new()
        {
            scaleMlp = 0.9f,
            handOffset = new Vector3(0, 0.01f, -0.1f),
            pelvisPositionWeight = 0,
            pelvisRotationWeight = 0
        };
        private List<AvatarNearClip> avatarNearClips = new();
        public FingerCalibration fingerCalibration;

        public bool IsCrouched { get; private set; }
        public bool IsCrawling { get; private set; }

        public LocalAvatarCreator(LocalPlayer localPlayer, CCK.Unity.Avatar a, bool isVR)
        {
            a = Object.Instantiate(a.gameObject).GetComponent<CCK.Unity.Avatar>();
            Avatar = a;
            SceneManager.MoveGameObjectToScene(a.gameObject, localPlayer.gameObject.scene);
            MainAnimator = a.GetComponent<Animator>();
            MainAnimator.updateMode = AnimatorUpdateMode.Normal;
            OnCreate(Avatar, 7);
            fingerCalibration = new FingerCalibration(this);
            HeadAlign = new GameObject("headalign_" + Guid.NewGuid());
            HeadAlign.transform.SetParent(a.ViewPosition.transform);
            HeadAlign.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            VoiceAlign = new GameObject("voicealign_" + Guid.NewGuid());
            VoiceAlign.transform.SetParent(a.SpeechPosition.transform);
            VoiceAlign.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            audioSource = VoiceAlign.AddComponent<AudioSource>();
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
                vrikSettings.headOffset = head.position - HeadAlign.transform.position;
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
                Calibrated = true;
            }
            if (string.IsNullOrEmpty(LocalPlayer.Instance.avatarMeta.ImageURL))
                CurrentAvatarBanner.Instance.Render(this, Array.Empty<byte>());
            else
                DownloadTools.DownloadBytes(LocalPlayer.Instance.avatarMeta.ImageURL,
                    bytes => CurrentAvatarBanner.Instance.Render(this, bytes));
            SetupLipSyncLocalPlayer();
        }

        private void SetupLipSyncLocalPlayer()
        {
            if (!Avatar.UseVisemes) return;
            lipSyncContext = VoiceAlign.AddComponent<OVRLipSyncContext>();
            lipSyncContext.audioSource = audioSource;
            lipSyncContext.enableKeyboardInput = false;
            lipSyncContext.enableTouchInput = false;
            lipSyncContext.skipAudioSource = true;
            morphTargets.Clear();
            foreach (KeyValuePair<Viseme, BlendshapeDescriptor> avatarVisemeRenderer in Avatar.VisemesDict)
            {
                OVRLipSyncContextMorphTarget morphTarget =
                    GetMorphTargetBySkinnedMeshRenderer(avatarVisemeRenderer.Value.SkinnedMeshRenderer);
                SetVisemeAsBlendshape(ref morphTarget, avatarVisemeRenderer.Key, avatarVisemeRenderer.Value);
            }
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

        internal void Update(bool areTwoTriggersClicked, Transform cameraTransform, Transform LeftHandReference, 
            Transform RightHandReference, bool isMoving, LocalPlayer localPlayer)
        {
            Update();
            switch (Calibrated)
            {
                case false:
                {
                    Transform t = HeadAlign.transform;
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
            if (vrik != null && vrik.solver.initiated && (!XRTracker.CanFBT || MainAnimator.avatar == null) && !Calibrated)
            {
                VRIKCalibrator.CalibrationData calibrationData = VRIKCalibrator.Calibrate(vrik, vrikSettings,
                    cameraTransform, null, LeftHandReference.transform, RightHandReference.transform);
                LocalPlayerSyncController.calibratedFBT = false;
                LocalPlayerSyncController.CalibrationData = JsonUtility.ToJson(calibrationData);
                vrik.solver.locomotion.stepThreshold = 0.01f;
                vrik.solver.locomotion.angleThreshold = 20;
                vrik.solver.plantFeet = false;
                SetupAnimators();
                Calibrated = true;
            }
            else if (vrik != null && XRTracker.CanFBT && !Calibrated)
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
                                VRIKCalibrator.CalibrationData calibrationData = VRIKCalibrator.Calibrate(vrik, vrikSettings,
                                    cameraTransform, newTs[0], LeftHandReference.transform,
                                    RightHandReference.transform, newTs[1], newTs[2]);
                                LocalPlayerSyncController.calibratedFBT = true;
                                LocalPlayerSyncController.CalibrationData = JsonUtility.ToJson(calibrationData);
                                RelaxWrists(GetBoneFromHumanoid(HumanBodyBones.LeftLowerArm),
                                    GetBoneFromHumanoid(HumanBodyBones.RightLowerArm), GetBoneFromHumanoid(HumanBodyBones.LeftHand),
                                    GetBoneFromHumanoid(HumanBodyBones.RightHand));
                                SetupAnimators();
                                Calibrated = true;
                            }
                        }
                    }
                }
            }
            else if (vrik != null && Calibrated)
            {
                vrik.solver.locomotion.weight = isMoving || XRTracker.CanFBT ? 0f : 1f;
                if (!XRTracker.CanFBT)
                {
                    float scale = LocalPlayer.Instance.transform.localScale.y;
                    float height = LocalPlayer.Instance.CharacterController.height;
                    vrik.solver.locomotion.footDistance = 0.1f * scale * height;
                    vrik.solver.locomotion.stepThreshold = 0.2f * scale * height;
                }
                MainAnimator.runtimeAnimatorController = animatorController;
                MainAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            else if (vrik == null)
            {
                MainAnimator.runtimeAnimatorController = animatorController;
                MainAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            if (vrik != null || !LocalPlayer.IsVR)
                fingerCalibration?.Update(localPlayer, localPlayer.GetLeftHandCurler(),
                    localPlayer.GetRightHandCurler());
        }

        internal void LateUpdate(bool isVR, Transform cameraTransform, bool lockCamera, bool driveCamera)
        {
            LateUpdate();
            if (!isVR && HeadAlign != null && !lockCamera)
            {
                cameraTransform.position = HeadAlign.transform.position;
                if(driveCamera) DriveCamera(cameraTransform);
            }
            if (!isVR)
            {
                new List<PathDescriptor>(LocalPlayer.Instance.SavedTransforms).ForEach(pathDescriptor =>
                {
                    if (pathDescriptor == null)
                        LocalPlayer.Instance.SavedTransforms.Remove(pathDescriptor);
                });
            }
        }

        internal void SetMove(Vector2 move, bool isRunning)
        {
            if (MainAnimator == null || !MainAnimator.isInitialized)
                return;
            MainAnimator.SetFloat("MoveX", move.x);
            MainAnimator.SetFloat("MoveY", move.y);
            MainAnimator.SetBool("Walking", !isRunning && !IsCrawling && !IsCrouched && (move.x != 0 || move.y != 0));
        }

        internal void SetRun(bool isRunning) => MainAnimator.SetBool("Running", isRunning);

        internal void Jump(bool isJumping) => MainAnimator.SetBool("Jump", isJumping);

        internal void SetCrouch(bool v)
        {
            if (IsCrawling) SetCrawl(false);
            MainAnimator.SetBool("Crouching", v);
            IsCrouched = v;
        }

        internal void SetCrawl(bool v)
        {
            if(IsCrouched) SetCrouch(false);
            MainAnimator.SetBool("Crawling", v);
            IsCrawling = v;
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
            // TODO: what am i doing
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

        internal void UpdateFace(Dictionary<string, float> weights)
        {
            if (FaceTrackingDescriptor == null)
            {
                foreach (string faceExpression in weights.Keys)
                    SetParameter(faceExpression, 0);
                return;
            }
            foreach (KeyValuePair<string,float> keyValuePair in weights)
            {
                try
                {
                    FaceExpressions faceExpressions =
                        (FaceExpressions) Enum.Parse(typeof(FaceExpressions), keyValuePair.Key);
                    if (!FaceTrackingDescriptor.FaceValues.ContainsKey(faceExpressions)) continue;
                    BlendshapeDescriptor blendshapeDescriptor = FaceTrackingDescriptor.FaceValues[faceExpressions];
                    if (blendshapeDescriptor != null && blendshapeDescriptor.SkinnedMeshRenderer != null)
                    {
                        SetBlendshapeWeight(blendshapeDescriptor.SkinnedMeshRenderer,
                            blendshapeDescriptor.BlendshapeIndex, keyValuePair.Value * 100);
                        SetParameter(keyValuePair.Key, keyValuePair.Value);
                    }
                    else
                        SetParameter(keyValuePair.Key, 0);
                }
                catch (Exception)
                {
                    // Was not a valid Enum, handle as Custom
                    SetParameter(keyValuePair.Key, keyValuePair.Value);
                }
            }
        }

        public override void Dispose()
        {
            LocalPlayerSyncController.CalibrationData = null;
            LocalPlayerSyncController.calibratedFBT = false;
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
            base.Dispose();
        }
    }
}