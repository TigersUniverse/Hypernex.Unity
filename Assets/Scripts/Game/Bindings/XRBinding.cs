using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

// ReSharper disable Unity.NoNullPropagation

namespace Hypernex.Game.Bindings
{
    public class XRBinding : IBinding
    {
        public string Id => (IsLook ? "Left" : "Right") + " VRController";
        public Transform AttachedObject =>
            IsLook ? LocalPlayer.Instance.RightHandReference : LocalPlayer.Instance.LeftHandReference;
        public bool IsLeftController => !IsLook;
        public bool IsRightController => IsLook;
        public bool IsLook { get; }
        public float Up { get; set; }
        public float Down { get; set; }
        public float Left { get; set; }
        public float Right { get; set; }
        public bool Button { get; set; }
        public Action ButtonClick { get; set; } = () => { };
        public bool Button2 { get; set; }
        public Action Button2Click { get; set; } = () => { };
        public float Trigger { get; set; }
        public Action TriggerClick { get; set; } = () => { };
        public bool Grab { get; set; }

        private XRHandSubsystem handSubsystem;
        
        public XRBinding(bool isLook) => IsLook = isLook;

        private float maxAngleForFullCurl = 90f;
        public bool AreFingersTracked { get; private set; }
        public float ThumbCurl { get; private set; }
        public float IndexCurl { get; private set; }
        public float MiddleCurl { get; private set; }
        public float RingCurl { get; private set; }
        public float PinkyCurl { get; private set; }

        public void Update()
        {
            // TODO: Get Index Fingers
            if (handSubsystem == null)
            {
                // Unity Docs say to use null propagations
                handSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
                if (handSubsystem != null)
                    handSubsystem.updatedHands += (subsystem, flags, updateType) =>
                    {
                        XRHand xrHand = IsLook ? subsystem.rightHand : subsystem.leftHand;
                        switch (updateType)
                        {
                            case XRHandSubsystem.UpdateType.Dynamic:
                                // game logic
                                Pose indexIntermediatePose;
                                Pose middleIntermediatePose;
                                Pose ringIntermediatePose;
                                Pose pinkyIntermediatePose;
                                Pose thumbProximalPose;
                                Pose indexTipPose;
                                Pose middleTipPose;
                                Pose ringTipPose;
                                Pose pinkyTipPose;
                                Pose thumbTipPose;
                                if (xrHand.GetJoint(XRHandJointID.IndexIntermediate).TryGetPose(out indexIntermediatePose) &&
                                    xrHand.GetJoint(XRHandJointID.MiddleIntermediate).TryGetPose(out middleIntermediatePose) &&
                                    xrHand.GetJoint(XRHandJointID.RingIntermediate).TryGetPose(out ringIntermediatePose) &&
                                    xrHand.GetJoint(XRHandJointID.LittleIntermediate).TryGetPose(out pinkyIntermediatePose) &&
                                    xrHand.GetJoint(XRHandJointID.ThumbProximal).TryGetPose(out thumbProximalPose) &&
                                    xrHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out indexTipPose) &&
                                    xrHand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out middleTipPose) &&
                                    xrHand.GetJoint(XRHandJointID.RingTip).TryGetPose(out ringTipPose) &&
                                    xrHand.GetJoint(XRHandJointID.LittleTip).TryGetPose(out pinkyTipPose) &&
                                    xrHand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out thumbTipPose))
                                {
                                    // Bend Finger goes here
                                    float indexAngle = Quaternion.Angle(indexIntermediatePose.rotation,
                                        indexTipPose.rotation);
                                    float middleAngle = Quaternion.Angle(middleIntermediatePose.rotation,
                                        middleTipPose.rotation);
                                    float ringAngle = Quaternion.Angle(ringIntermediatePose.rotation,
                                        ringTipPose.rotation);
                                    float pinkyAngle = Quaternion.Angle(pinkyIntermediatePose.rotation,
                                        pinkyTipPose.rotation);
                                    float thumbAngle = Quaternion.Angle(thumbProximalPose.rotation,
                                        thumbTipPose.rotation);
                                    float normalIndexAngle = Mathf.Clamp(indexAngle / maxAngleForFullCurl, 0, 1);
                                    float normalMiddleAngle = Mathf.Clamp(middleAngle / maxAngleForFullCurl, 0, 1);
                                    float normalRingAngle = Mathf.Clamp(ringAngle / maxAngleForFullCurl, 0, 1);
                                    float normalPinkyAngle = Mathf.Clamp(pinkyAngle / maxAngleForFullCurl, 0, 1);
                                    float normalThumbAngle = Mathf.Clamp(thumbAngle / maxAngleForFullCurl, 0, 1);
                                    ThumbCurl = 1 - normalThumbAngle;
                                    IndexCurl = 1 - normalIndexAngle;
                                    MiddleCurl = 1 - normalMiddleAngle;
                                    RingCurl = 1 - normalRingAngle;
                                    PinkyCurl = 1 - normalPinkyAngle;
                                }
                                break;
                            case XRHandSubsystem.UpdateType.BeforeRender:
                                // visual objects
                                break;
                        }
                    };
            }
            else
                AreFingersTracked = IsLook ? handSubsystem.rightHand.isTracked : handSubsystem.leftHand.isTracked;

            /*Logger.CurrentLogger.Log(Id + " : " + ThumbCurl + ", " + IndexCurl + ", " + MiddleCurl + ", " + RingCurl +
                                     ", " + PinkyCurl);*/
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            //if (IsLeftController)
                //return;
            Vector2 move = context.ReadValue<Vector2>();
            switch (move.x)
            {
                case > 0:
                    Right = move.x;
                    Left = 0;
                    break;
                case < 0:
                    Right = 0;
                    Left = -1 * move.x;
                    break;
                default:
                    Right = 0;
                    Left = 0;
                    break;
            }
            switch (move.y)
            {
                case > 0:
                    Up = move.y;
                    Down = 0;
                    break;
                case < 0:
                    Up = 0;
                    Down = -1 * move.y;
                    break;
                default:
                    Up = 0;
                    Down = 0;
                    break;
            }
        }

        public void OnTurn(InputAction.CallbackContext context)
        {
            //if(IsRightController)
                //return;
            Vector2 move = context.ReadValue<Vector2>();
            switch (move.x)
            {
                case > 0:
                    Right = move.x;
                    Left = 0;
                    break;
                case < 0:
                    Right = 0;
                    Left = -1 * move.x;
                    break;
                default:
                    Right = 0;
                    Left = 0;
                    break;
            }
            switch (move.y)
            {
                case > 0:
                    Up = move.y;
                    Down = 0;
                    break;
                case < 0:
                    Up = 0;
                    Down = -1 * move.y;
                    break;
                default:
                    Up = 0;
                    Down = 0;
                    break;
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            //if (IsRightController)
                //return;
            bool value = context.ReadValue<float>() >= 0.99f;
            if(!Button && value)
                ButtonClick.Invoke();
            Button = value;
        }

        public void OnDashboard(InputAction.CallbackContext context)
        {
            //if (IsLeftController)
                //return;
            bool value = context.ReadValue<float>() >= 0.99f;
            if(!Button2 && value)
                Button2Click.Invoke();
            Button2 = value;
        }

        public void OnAction(InputAction.CallbackContext context)
        {
            //if (IsLeftController)
                //return;
            bool value = context.ReadValue<float>() >= 0.99f;
            if(!Button && value)
                ButtonClick.Invoke();
            Button = value;
        }

        public void OnToggleMicrophone(InputAction.CallbackContext context)
        {
            //if (IsLeftController)
                //return;
            bool value = context.ReadValue<float>() >= 0.99f;
            if(!Button2 && value)
                Button2Click.Invoke();
            Button2 = value;
        }

        public void OnPrimaryClick(InputAction.CallbackContext context)
        {
            //if (IsRightController)
                //return;
            float value = context.ReadValue<float>();
            if(Trigger <= 0.05f && value > 0.05f)
                TriggerClick.Invoke();
            Trigger = value;
        }

        public void OnSecondaryClick(InputAction.CallbackContext context)
        {
            //if (IsLeftController)
                //return;
            float value = context.ReadValue<float>();
            if(Trigger <= 0.05f && value > 0.05f)
                TriggerClick.Invoke();
            Trigger = value;
        }

        public void OnLeftGrab(InputAction.CallbackContext context)
        {
            //if (IsLeftController)
                //return;
            Grab = context.ReadValue<float>() >= 0.9f;
        }

        public void OnRightGrab(InputAction.CallbackContext context)
        {
            //if(IsRightController)
                //return;
            Grab = context.ReadValue<float>() >= 0.9f;
        }

        public static List<(string, float)> GetFingerTrackingWeights(XRBinding left, XRBinding right)
        {
            List<(string, float)> weights = new List<(string, float)>();
            weights.Add(("LeftThumbCurl", left.ThumbCurl));
            weights.Add(("LeftIndexCurl", left.IndexCurl));
            weights.Add(("LeftMiddleCurl", left.MiddleCurl));
            weights.Add(("LeftRingCurl", left.RingCurl));
            weights.Add(("LeftPinkyCurl", left.PinkyCurl));
            weights.Add(("RightThumbCurl", right.ThumbCurl));
            weights.Add(("RightIndexCurl", right.IndexCurl));
            weights.Add(("RightMiddleCurl", right.MiddleCurl));
            weights.Add(("RightRingCurl", right.RingCurl));
            weights.Add(("RightPinkyCurl", right.PinkyCurl));
            return weights;
        }
    }
}