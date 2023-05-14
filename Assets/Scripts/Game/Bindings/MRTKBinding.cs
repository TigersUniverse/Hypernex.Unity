using System;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypernex.Game.Bindings
{
    public class MRTKBinding : IBinding, VRBindings.IUnknownActions, VRBindings.IIndexActions, VRBindings.IOculusActions
    {
        public string Id => handedness + " VRController";
        public bool IsLook { get; }
        public float Up { get; set; }
        public float Down { get; set; }
        public float Left { get; set; }
        public float Right { get; set; }
        public bool Button { get; set; }
        public Action ButtonClick { get; set; }
        public bool Button2 { get; set; }
        public Action Button2Click { get; set; }
        public float Trigger { get; set; }
        public Action TriggerClick { get; set; }

        private Handedness handedness;
        private VRBindings vrBindings;
        
        public MRTKBinding(Handedness handedness, VRBindings vrBindings)
        {
            this.handedness = handedness;
            IsLook = this.handedness == Handedness.Right;
            this.vrBindings = vrBindings;
            vrBindings.Unknown.SetCallbacks(this);
            vrBindings.Index.SetCallbacks(this);
            vrBindings.Oculus.SetCallbacks(this);
        }
        
        public float ThumbCurl { get; private set; }
        public float IndexCurl { get; private set; }
        public float MiddleCurl { get; private set; }
        public float RingCurl { get; private set; }
        public float PinkyCurl { get; private set; }
        
        public void Update()
        {
            ThumbCurl = HandPoseUtils.ThumbFingerCurl(handedness);
            IndexCurl = HandPoseUtils.IndexFingerCurl(handedness);
            MiddleCurl = HandPoseUtils.MiddleFingerCurl(handedness);
            RingCurl = HandPoseUtils.RingFingerCurl(handedness);
            PinkyCurl = HandPoseUtils.PinkyFingerCurl(handedness);
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (IsLook)
                return;
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
            if(!IsLook)
                return;
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
            if (!IsLook)
                return;
            bool value = context.ReadValue<bool>();
            if(!Button && value)
                ButtonClick.Invoke();
            Button = value;
        }

        public void OnDashboard(InputAction.CallbackContext context)
        {
            if (!IsLook)
                return;
            bool value = context.ReadValue<bool>();
            if(!Button2 && value)
                Button2Click.Invoke();
            Button2 = value;
        }

        public void OnAction(InputAction.CallbackContext context)
        {
            if (IsLook)
                return;
            bool value = context.ReadValue<bool>();
            if(!Button && value)
                ButtonClick.Invoke();
            Button = value;
        }

        public void OnToggleMicrophone(InputAction.CallbackContext context)
        {
            if (IsLook)
                return;
            bool value = context.ReadValue<bool>();
            if(!Button2 && value)
                Button2Click.Invoke();
            Button2 = value;
        }

        public void OnPrimaryClick(InputAction.CallbackContext context)
        {
            if (!IsLook)
                return;
            bool value = context.ReadValue<bool>();
            if(Trigger <= 0.05f && value)
                TriggerClick.Invoke();
            Trigger = value ? 1f : 0f;
        }

        public void OnSecondaryClick(InputAction.CallbackContext context)
        {
            if (IsLook)
                return;
            bool value = context.ReadValue<bool>();
            if(Trigger <= 0.05f && value)
                TriggerClick.Invoke();
            Trigger = value ? 1f : 0f;
        }
    }
}