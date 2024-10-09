using System;
using System.Collections.Generic;
using Hypernex.CCK;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Configuration;
using Hypernex.Databasing;
using Hypernex.Databasing.Objects;
using Hypernex.ExtendedTracking;
using Hypernex.Game.Bindings;
using Hypernex.Game.Networking;
using Hypernex.Networking.Messages;
using Hypernex.Sandboxing;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRCFaceTracking.Core.Params.Data;
using Object = UnityEngine.Object;

namespace Hypernex.Game.Avatar
{
    public class LocalAvatarCreator : AvatarCreator
    {
        private List<AvatarNearClip> avatarNearClips = new();
        private readonly AllowedAvatarComponent allowedAvatarComponent = new(true, true, true, true, true, true);
        public FingerCalibration fingerCalibration;
        public readonly AvatarConfiguration AvatarConfiguration;

        public bool IsCrouched { get; private set; }
        public bool IsCrawling { get; private set; }

        public LocalAvatarCreator(LocalPlayer localPlayer, CCK.Unity.Avatar a, bool isVR, AvatarMeta avatarMeta)
        {
            AvatarConfiguration = ConfigManager.GetDatabase()
                .Get<AvatarConfiguration>(AvatarConfiguration.TABLE, avatarMeta.Id);
            AvatarConfiguration ??= ConfigManager.GetDatabase()
                .Insert(AvatarConfiguration.TABLE, new AvatarConfiguration(avatarMeta));
            a = Object.Instantiate(a.gameObject).GetComponent<CCK.Unity.Avatar>();
            Avatar = a;
            SceneManager.MoveGameObjectToScene(a.gameObject, localPlayer.gameObject.scene);
            MainAnimator = a.GetComponent<Animator>();
            MainAnimator.updateMode = AnimatorUpdateMode.Normal;
            HeadAlign = new GameObject("headalign_" + Guid.NewGuid());
            HeadAlign.transform.SetParent(a.ViewPosition.transform);
            HeadAlign.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            VoiceAlign = new GameObject("voicealign_" + Guid.NewGuid());
            VoiceAlign.transform.SetParent(a.SpeechPosition.transform);
            VoiceAlign.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            audioSource = VoiceAlign.AddComponent<AudioSource>();
            OnCreate(Avatar, 7, allowedAvatarComponent, avatarMeta);
            fingerCalibration = new FingerCalibration(this);
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in
                     a.transform.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (!skinnedMeshRenderer.name.Contains("shadowclone_"))
                {
                    AvatarNearClip avatarNearClip = skinnedMeshRenderer.gameObject.AddComponent<AvatarNearClip>();
                    if(avatarNearClip != null && avatarNearClip.Setup(this, localPlayer.Camera))
                        avatarNearClips.Add(avatarNearClip);
                }
            avatarNearClips.ForEach(x => x.CreateShadows());
            a.gameObject.name = "avatar";
            a.transform.SetParent(localPlayer.transform, true);
            AlignAvatar(isVR);
            a.transform.localScale = Vector3.one;
            if (isVR)
            {
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
                vrik = AddVRIK(Avatar.gameObject);
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
            VRCFTParameters.UpdateParameters(avatarMeta, this);
            GameInstance.OnGameInstanceLoaded += OnGameInstanceLoaded;
            GameInstance.OnGameInstanceDisconnect += OnGameInstanceDisconnect;
            LoadScripts();
        }
        
        private void OnGameInstanceLoaded(GameInstance arg1, WorldMeta arg2, Scene arg3) => LoadScripts();
        private void OnGameInstanceDisconnect(GameInstance arg1) => LoadScripts();

        private List<(GameObject, NexboxScript)> localAvatarScripts;

        private void LoadScripts()
        {
            DisposeScripts();
            if (localAvatarScripts == null)
            {
                localAvatarScripts = new List<(GameObject, NexboxScript)>();
                foreach (NexboxScript localAvatarScript in Avatar.LocalAvatarScripts)
                    localAvatarScripts.Add((Avatar.gameObject, localAvatarScript));
                foreach (LocalScript ls in Avatar.gameObject.GetComponentsInChildren<LocalScript>())
                    localAvatarScripts.Add((ls.gameObject, ls.NexboxScript));
            }
            foreach ((GameObject, NexboxScript) avatarScript in localAvatarScripts)
                localAvatarSandboxes.Add(new Sandbox(avatarScript.Item2, LocalPlayer.Instance.transform, avatarScript.Item1));
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
                LeftHandReference.ClearChildren(true);
                RightHandReference.ClearChildren(true);
                LocalPlayerSyncController.calibratedFBT = false;
                LocalPlayerSyncController.CalibrationData = JsonUtility.ToJson(CalibrateVRIK(cameraTransform, LeftHandReference, RightHandReference));
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
                                LeftHandReference.ClearChildren(true);
                                RightHandReference.ClearChildren(true);
                                newTs[0].ClearChildren(true);
                                newTs[1].ClearChildren(true);
                                newTs[2].ClearChildren(true);
                                newTs[0].rotation = body.rotation;
                                newTs[1].rotation = leftFoot.rotation;
                                newTs[2].rotation = rightFoot.rotation;
                                LocalPlayerSyncController.calibratedFBT = true;
                                LocalPlayerSyncController.CalibrationData = JsonUtility.ToJson(
                                    CalibrateVRIK(cameraTransform, newTs[0], LeftHandReference, RightHandReference,
                                        newTs[1], newTs[2]));
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
                UpdateVRIK(XRTracker.CanFBT, isMoving, LocalPlayer.Instance.transform.localScale.y);
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
        
        internal void SaveAvatarConfiguration()
        {
            Database database = ConfigManager.GetDatabase();
            if(database == null)
            {
                CCK.Logger.CurrentLogger.Error("No database loaded!");
                return;
            }
            database.Insert(AvatarConfiguration.TABLE, AvatarConfiguration);
        }

        protected sealed override void SetupAnimators()
        {
            base.SetupAnimators();
            if(string.IsNullOrEmpty(AvatarConfiguration.SelectedWeight)) return;
            if (!AvatarConfiguration.SavedWeights.TryGetValue(AvatarConfiguration.SelectedWeight,
                   out WeightedObjectUpdate[] weights)) return;
            SetParameters(weights);
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

        internal void UpdateFace(Dictionary<string, (float, ICustomFaceExpression)> weights)
        {
            if (FaceTrackingDescriptor == null)
            {
                foreach (string faceExpression in weights.Keys)
                    SetParameter(faceExpression, 0);
                return;
            }
            foreach (KeyValuePair<string, (float, ICustomFaceExpression)> keyValuePair in weights)
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
                            blendshapeDescriptor.BlendshapeIndex, keyValuePair.Value.Item1 * 100);
                        SetParameter(keyValuePair.Key, keyValuePair.Value.Item1);
                    }
                    else
                        SetParameter(keyValuePair.Key, 0);
                }
                catch (Exception)
                {
                    // Was not a valid Enum, handle as Custom
                    ICustomFaceExpression customFaceExpression = keyValuePair.Value.Item2;
                    // This should NEVER happen, but if it does, we don't want to error
                    if(customFaceExpression == null) continue;
                    foreach (AnimatorControllerParameter animatorControllerParameter in Parameters)
                    {
                        // Check if the parameter name matches the expression name
                        if(!customFaceExpression.IsMatch(animatorControllerParameter.name)) continue;
                        // Set the parameter value
                        SetParameter(keyValuePair.Key, keyValuePair.Value.Item1);
                    }
                }
            }
        }

        public override void Dispose()
        {
            LocalPlayerSyncController.CalibrationData = null;
            LocalPlayerSyncController.calibratedFBT = false;
            foreach (string s in new List<string>(Sandboxing.SandboxedTypes.Player.AssignedTags))
            {
                foreach (string morePlayerAssignedTag in new List<string>(LocalPlayer.MorePlayerAssignedTags))
                {
                    if (s == morePlayerAssignedTag)
                        LocalPlayer.MorePlayerAssignedTags.Remove(morePlayerAssignedTag);
                }
            }
            foreach (string s in new List<string>(Sandboxing.SandboxedTypes.Player.ExtraneousKeys))
            {
                foreach (KeyValuePair<string, object> extraneousObject in new Dictionary<string, object>(LocalPlayer
                             .MoreExtraneousObjects))
                    if (s == extraneousObject.Key)
                        LocalPlayer.MoreExtraneousObjects.Remove(extraneousObject.Key);
            }
            GameInstance.OnGameInstanceLoaded -= OnGameInstanceLoaded;
            GameInstance.OnGameInstanceDisconnect -= OnGameInstanceDisconnect;
            base.Dispose();
        }
    }
}