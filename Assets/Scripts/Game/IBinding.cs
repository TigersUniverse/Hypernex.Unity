using System;

namespace Hypernex.Game
{
    public interface IBinding
    {
        public string Id { get; }
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
        /// Button (Like ABXY)
        /// </summary>
        public bool Button { get; set; }
        /// <summary>
        /// Invoked when the Button is clicked
        /// </summary>
        public Action ButtonClick { get; set; }
        /// <summary>
        /// Trigger (Like Right Trigger/Left Click)
        /// </summary>
        public float Trigger { get; set; }
        /// <summary>
        /// Invoked when the Trigger is clicked
        /// </summary>
        public Action TriggerClick { get; set; }

        public void Update();
    }
}