using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.Game.Bindings
{
    public class Keyboard : IBinding
    {
        public string Id => "Keyboard";
        public bool IsLook => false;
        
        public float Up { get; set; }
        public float Down { get; set; }
        public float Left { get; set; }
        public float Right { get; set; }
        public bool Button { get; set; }
        public Action ButtonClick { get; set; } = () => { };
        public float Trigger { get; set; }
        public Action TriggerClick { get; set; } = () => { };

        private static Dictionary<KeyCode, Action> customEvents = new();

        public void Update()
        {
            Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            switch (move.x)
            {
                case > 0:
                    Up = move.x;
                    Down = 0;
                    break;
                case < 0:
                    Up = 0;
                    Down = -1 * move.x;
                    break;
                default:
                    Up = 0;
                    Down = 0;
                    break;
            }
            switch (move.y)
            {
                case > 0:
                    Right = move.y;
                    Left = 0;
                    break;
                case < 0:
                    Right = 0;
                    Left = -1 * move.y;
                    break;
                default:
                    Right = 0;
                    Left = 0;
                    break;
            }
            Button = Input.GetKey(KeyCode.E);
            if(Input.GetKeyDown(KeyCode.E))
                ButtonClick.Invoke();
            if(Input.GetMouseButtonDown(0))
                TriggerClick.Invoke();
            Trigger = Input.GetMouseButton(0) ? 1.0f : 0;
            foreach (KeyValuePair<KeyCode, Action> keyValuePair in new Dictionary<KeyCode, Action>(customEvents))
            {
                if(Input.GetKeyDown(keyValuePair.Key))
                    keyValuePair.Value.Invoke();
            }
        }

        public Keyboard RegisterCustomKeyDownEvent(KeyCode keyCode, Action a)
        {
            if (customEvents.ContainsKey(keyCode))
                customEvents.Remove(keyCode);
            customEvents.Add(keyCode, a);
            return this;
        }
    }
}