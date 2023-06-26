using System;
using UnityEngine;

namespace Hypernex.Game
{
    public interface IBinding
    {
        /// <summary>
        /// An Identifier for each IBinding
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// Used for Binding Raycasts and anything else
        /// </summary>
        public Transform AttachedObject { get; }
        /// <summary>
        /// Is this device used for looking? (This would be true for a mouse)
        /// </summary>
        public bool IsLook { get; }
        /// <summary>
        /// Move Up
        /// </summary>
        public float Up { get; set; }
        /// <summary>
        /// Move Down
        /// </summary>
        public float Down { get; set; }
        /// <summary>
        /// Move Left
        /// </summary>
        public float Left { get; set; }
        /// <summary>
        /// Move Right
        /// </summary>
        public float Right { get; set; }
        
        /// <summary>
        /// Button (Like AX)
        /// </summary>
        public bool Button { get; set; }
        /// <summary>
        /// Invoked when the Button is clicked
        /// </summary>
        public Action ButtonClick { get; set; }
        /// <summary>
        /// Button (Like BY)
        /// </summary>
        public bool Button2 { get; set; }
        /// <summary>
        /// Invoked when the Button2 is clicked
        /// </summary>
        public Action Button2Click { get; set; }
        /// <summary>
        /// Trigger (Like Right Trigger/Left Click)
        /// </summary>
        public float Trigger { get; set; }
        /// <summary>
        /// Invoked when the Trigger is clicked
        /// </summary>
        public Action TriggerClick { get; set; }
        /// <summary>
        /// If the specified Grab binding is being held
        /// </summary>
        public bool Grab { get; set; }

        public void Update();
    }
}