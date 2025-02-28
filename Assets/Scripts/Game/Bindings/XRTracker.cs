using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Game.Avatar;
using Hypernex.Networking.Messages.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypernex.Game.Bindings
{
    public class XRTracker : MonoBehaviour
    {
        private const int MAX_STEPS = 100;
        
        public static List<XRTracker> Trackers => new(_trackers);
        private static readonly List<XRTracker> _trackers = new();

        public static bool CanFBT => Trackers.Count(x => x.IsTracked && x.TrackerRole != XRTrackerRole.Camera) == 3;

        public XRTrackerRole TrackerRole;
        public bool IsTracked { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public CoreBone? CalibratedTo { get; set; }

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

        public void OnIsTracked(InputAction.CallbackContext context)
        {
            bool value = context.ReadValueAsButton();
            if (value) IsTracked = true;
        }

        public void OnPosition(InputAction.CallbackContext context)
        {
            Position = context.ReadValue<Vector3>();
            Step();
        }

        public void OnRotation(InputAction.CallbackContext context)
        {
            Rotation = context.ReadValue<Quaternion>();
            Step();
        }

        private void Show()
        {
            if(renderer == null)
                return;
            renderer.enabled = true;
        }
        private void Hide()
        {
            if(renderer == null)
                return;
            renderer.enabled = false;
        }

        private int stepsWithoutTracking;

        private void Step()
        {
            stepsWithoutTracking = 0;
            IsTracked = true;
        }

        private void Start() => _trackers.Add(this);

        private void FixedUpdate()
        {
            if (stepsWithoutTracking > MAX_STEPS) IsTracked = false;
            if(stepsWithoutTracking == Int32.MaxValue) return;
            stepsWithoutTracking++;
        }

        private void Update()
        {
            if (!IsTracked)
            {
                Hide();
                return;
            }
            if (TrackerRole == XRTrackerRole.Camera)
            {
                if (HandleCamera.allCameras.Count(x => x.AttachedToTracker) > 0)
                {
                    Hide();
                    return;
                }
                Show();
                return;
            }
            LocalPlayer localPlayer = LocalPlayer.Instance;
            if (localPlayer == null)
            {
                Show();
                return;
            }
            AvatarCreator avatarCreator = localPlayer.avatar;
            if (avatarCreator == null)
            {
                Show();
                return;
            }
            if (!avatarCreator.Calibrated)
            {
                Show();
                return;
            }
            Hide();
        }
    }

    public enum XRTrackerRole
    {
        LeftFoot,
        RightFoot,
        Hip,
        Camera
    }
}