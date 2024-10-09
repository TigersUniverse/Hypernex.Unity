using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.Game.Bindings
{
    public class Keyboard : IBinding, IDisposable
    {
        public string Id => "Keyboard";
        public Transform AttachedObject => LocalPlayer.Instance.Camera.transform;
        public bool IsLook => false;
        
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
        // This grab will never be used
        public bool Grab { get; set; }

        private static Dictionary<KeyCode, List<Action>> customEvents = new();

        public void Update()
        {
            Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
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
            Button = Input.GetKey(KeyCode.E);
            if(Input.GetKeyDown(KeyCode.E))
                ButtonClick.Invoke();
            Button2 = Input.GetKey(KeyCode.LeftShift);
            if(Input.GetKeyDown(KeyCode.LeftShift))
                Button2Click.Invoke();
            if(Input.GetMouseButtonDown(0))
                TriggerClick.Invoke();
            Trigger = Input.GetMouseButton(0) ? 1.0f : 0;
            foreach (KeyValuePair<KeyCode, List<Action>> keyValuePair in new Dictionary<KeyCode, List<Action>>(customEvents))
            {
                if(Input.GetKeyDown(keyValuePair.Key))
                    foreach (Action action in new List<Action>(keyValuePair.Value))
                        action.Invoke();
            }
        }

        public Keyboard RegisterCustomKeyDownEvent(KeyCode keyCode, Action a)
        {
            if (!customEvents.ContainsKey(keyCode))
                customEvents.Add(keyCode, new List<Action>{a});
            else
                customEvents[keyCode].Add(a);
            return this;
        }

        public void RemoveCustomKeyDownEvent(KeyCode keyCode, Action a)
        {
            if(!customEvents.ContainsKey(keyCode)) return;
            customEvents[keyCode].RemoveAll(x => x == a);
        }

        public void Dispose() => customEvents.Clear();
    }
}