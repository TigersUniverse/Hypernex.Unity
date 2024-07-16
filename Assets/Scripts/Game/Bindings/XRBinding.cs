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
            !IsLook ? LocalPlayer.Instance.RightHandReference : LocalPlayer.Instance.LeftHandReference;
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

        private HandGetter HandGetter;

        public static GameObject GetControllerModel(Transform handReference)
        {
            for (int i = 0; i < handReference.childCount; i++)
            {
                Transform child = handReference.GetChild(i);
                if(!child.name.ToLower().Contains("model")) continue;
                return child.gameObject;
            }
            return null;
        }

        public XRBinding(bool isLook, HandGetter handGetter)
        {
            IsLook = isLook;
            HandGetter = handGetter;
        }

        private float maxAngleForFullCurl = 90f;
        public bool AreFingersTracked { get; private set; }
        public float ThumbCurl { get; private set; }
        public float IndexCurl { get; private set; }
        public float MiddleCurl { get; private set; }
        public float RingCurl { get; private set; }
        public float PinkyCurl { get; private set; }

        public void Update()
        {
            AreFingersTracked = HandGetter.Curls.Length == 5;
            if (!AreFingersTracked)
                return;
            ThumbCurl = HandGetter.Curls[0];
            IndexCurl = HandGetter.Curls[1];
            MiddleCurl = HandGetter.Curls[2];
            RingCurl = HandGetter.Curls[3];
            PinkyCurl = HandGetter.Curls[4];
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
    }
}