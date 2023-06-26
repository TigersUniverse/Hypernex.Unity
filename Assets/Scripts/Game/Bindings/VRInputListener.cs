using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypernex.Game.Bindings
{
    public class VRInputListener : MonoBehaviour
    {
        private List<XRBinding> xrBindings = new ();
        public List<XRBinding> XRBindings => new (xrBindings);
        public bool UseSnapTurn = true;
        public float TurnDegree = 45f;

        internal void AddXRBinding(XRBinding x) => xrBindings.Add(x);

        private XRBinding LeftController => XRBindings[1];
        private XRBinding RightController => XRBindings[0];

        public void OnMove(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnMove(context);
            LeftController.OnMove(context);
        }

        public void OnTurn(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnTurn(context);
            RightController.OnTurn(context);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnJump(context);
            RightController.OnJump(context);
        }

        public void OnDashboard(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnDashboard(context);
            RightController.OnDashboard(context);
        }

        public void OnAction(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnAction(context);
            LeftController.OnAction(context);
        }

        public void OnToggleMicrophone(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnToggleMicrophone(context);
            LeftController.OnToggleMicrophone(context);
        }

        public void OnPrimaryClick(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnPrimaryClick(context);
            RightController.OnPrimaryClick(context);
        }

        public void OnSecondaryClick(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnSecondaryClick(context);
            LeftController.OnSecondaryClick(context);
        }

        public void OnLeftGrab(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnLeftGrab(context);
            LeftController.OnLeftGrab(context);
        }

        public void OnRightGrab(InputAction.CallbackContext context)
        {
            //foreach (XRBinding xrBinding in XRBindings)
                //xrBinding.OnRightGrab(context);
            RightController.OnRightGrab(context);
        }
    }
}