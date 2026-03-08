using System;
using Hypernex.UI.Components;
using Unity.Mathematics;
using UnityEngine;

namespace Hypernex.Game.Bindings
{
    public class Mobile : IBinding, IBindingSensitivity
    {
        public string Id => "Mobile-" + (IsLook ? "look" : "move");

        public Transform AttachedObject => mobileControls.transform;
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

        public float Sensitivity { get; set; } = 5f;

        private MobileControls mobileControls;
        private Vector2 lastPos;
        private bool dragging;

        public Mobile(bool look, MobileControls mobileControls)
        {
            this.mobileControls = mobileControls;
            IsLook = look;
            // Move
            mobileControls.OnRunButton += OnRunButton;
            mobileControls.RightClick += OnRightClick;
            // Look
            mobileControls.OnJumpButton += OnJumpClick;
            mobileControls.OnMenuButton += OnMenuClick;
            mobileControls.LeftClick += OnLeftClick;
        }

        // Move
        private void OnRunButton()
        {
            if(IsLook) return;
            Button2Click.Invoke();
        }
        private void OnRightClick() => TriggerClick.Invoke();
        // Look
        private void OnJumpClick() => ButtonClick.Invoke();
        private void OnMenuClick() => Button2Click.Invoke();
        private void OnLeftClick() => TriggerClick.Invoke();
        
        private bool InLookZone(Vector2 pos)
        {
            float minX = Screen.width * 0.2f;
            float maxX = Screen.width * 0.8f;
            float minY = Screen.height * 0.2f;
            float maxY = Screen.height * 0.8f;
            return pos.x > minX && pos.x < maxX && pos.y > minY && pos.y < maxY;
        }

        private void Look()
        {
            if (Input.touchCount == 0)
            {
                dragging = false;
                Up = Down = Left = Right = 0;
                return;
            }
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                if (!InLookZone(t.position))
                {
                    dragging = false;
                    return;
                }
                dragging = true;
                lastPos = t.position;
                return;
            }
            if (!dragging) return;
            Vector2 delta = t.position - lastPos;
            lastPos = t.position;
            float dx = delta.x * 0.01f * Sensitivity;
            // give it a small boost
            float dy = delta.y * 0.01f * Sensitivity * 1.5f;
            if (dx > 0)
            {
                Right = dx;
                Left = 0;
            }
            else if (dx < 0)
            {
                Left = -dx;
                Right = 0;
            }
            else
            {
                Left = Right = 0;
            }
            if (dy > 0)
            {
                Up = dy;
                Down = 0;
            }
            else if (dy < 0)
            {
                Down = -dy;
                Up = 0;
            }
            else
            {
                Up = Down = 0;
            }
        }
        
        public void Update()
        {
            if (IsLook)
            {
                Look();
                Button = mobileControls.JumpButton;
                Button2 = mobileControls.MenuButton;
                Trigger = mobileControls.RightClickDown ? 1.0f : 0f;
                Grab = mobileControls.RightClickDown;
            }
            else
            {
                Up = math.max(0, mobileControls.Move.y);
                Down = math.abs(math.min(0, mobileControls.Move.y));
                Right = math.max(0, mobileControls.Move.x);
                Left = math.abs(math.min(0, mobileControls.Move.x));
                Button2 = mobileControls.RunButton;
                Trigger = mobileControls.LeftClickDown ? 1.0f : 0f;
            }
        }

        ~Mobile()
        {
            // Move
            mobileControls.OnRunButton -= OnRunButton;
            mobileControls.RightClick -= OnRightClick;
            // Look
            mobileControls.OnJumpButton -= OnJumpClick;
            mobileControls.OnMenuButton -= OnMenuClick;
            mobileControls.LeftClick -= OnLeftClick;
        }
    }
}