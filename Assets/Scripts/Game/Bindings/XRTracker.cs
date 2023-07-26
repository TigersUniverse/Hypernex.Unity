using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypernex.Game.Bindings
{
    public class XRTracker : MonoBehaviour
    {
        public static List<XRTracker> Trackers => new(_trackers);
        private static readonly List<XRTracker> _trackers = new();

        public XRTrackerRole TrackerRole;
        
        public bool IsTracked { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

        private bool lookedForR;
        private Renderer r;

        public new Renderer renderer
        {
            get
            {
                if (r == null && !lookedForR)
                {
                    r = GetComponent<Renderer>();
                    lookedForR = true;
                }
                return r;
            }
        }

        public void OnIsTracked(InputAction.CallbackContext context) => IsTracked = context.ReadValueAsButton();
        public void OnPosition(InputAction.CallbackContext context) => Position = context.ReadValue<Vector3>();
        public void OnRotation(InputAction.CallbackContext context) => Rotation = context.ReadValue<Quaternion>();

        private void Start() => _trackers.Add(this);
    }

    public enum XRTrackerRole
    {
        LeftFoot,
        RightFoot,
        Hip
    }
}