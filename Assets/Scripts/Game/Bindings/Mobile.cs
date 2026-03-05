using System;
using Hypernex.UI.Components;
using Unity.Mathematics;
using UnityEngine;

namespace Hypernex.Game.Bindings
{
    public class Mobile : IBinding
    {
        public string Id => "Mobile-" + (IsLook ? "look" : "move");
        public Transform AttachedObject => MobileControls.Instance.transform;
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

        public Mobile(bool look)
        {
            IsLook = look;
            // Move
            MobileControls.Instance.OnRunButton += OnRunButton;
            MobileControls.Instance.RightClick += OnRightClick;
            // Look
            MobileControls.Instance.OnJumpButton += OnJumpClick;
            MobileControls.Instance.OnMenuButton += OnMenuClick;
            MobileControls.Instance.LeftClick += OnLeftClick;
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
        
        public void Update()
        {
            if (IsLook)
            {
                Button = MobileControls.Instance.JumpButton;
                Button2 = MobileControls.Instance.MenuButton;
                Trigger = MobileControls.Instance.RightClickDown ? 1.0f : 0f;
                Grab = MobileControls.Instance.RightClickDown;
            }
            else
            {
                Up = math.max(0, MobileControls.Instance.Move.y);
                Down = math.abs(math.min(0, MobileControls.Instance.Move.y));
                Right = math.max(0, MobileControls.Instance.Move.x);
                Left = math.abs(math.min(0, MobileControls.Instance.Move.x));
                Button2 = MobileControls.Instance.RunButton;
                Trigger = MobileControls.Instance.LeftClickDown ? 1.0f : 0f;
            }
        }

        ~Mobile()
        {
            // Move
            MobileControls.Instance.OnRunButton -= OnRunButton;
            MobileControls.Instance.RightClick -= OnRightClick;
            // Look
            MobileControls.Instance.OnJumpButton -= OnJumpClick;
            MobileControls.Instance.OnMenuButton -= OnMenuClick;
            MobileControls.Instance.LeftClick -= OnLeftClick;
        }
    }
}