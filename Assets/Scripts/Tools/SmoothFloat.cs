using UnityEngine;

namespace Hypernex.Tools
{
    /// <summary>
    /// A tool that drives values of a float smoothly
    /// </summary>
    public class SmoothFloat
    {
        public float InterpolationFramesCount => interpolate.HasValue ? interpolate.Value : Init.Instance.SmoothingFrames;
        public float Value
        {
            get => currentValue;
            set => this.value = value;
        }

        private float? interpolate;
        private float currentValue;
        private float value;
        private float lastValue;

        internal SmoothFloat(){}

        internal SmoothFloat(float startingValue)
        {
            Value = startingValue;
            currentValue = startingValue;
            lastValue = startingValue;
        }

        internal SmoothFloat(float startingValue, float interpolation)
        {
            Value = startingValue;
            currentValue = startingValue;
            lastValue = startingValue;
            interpolate = interpolation;
        }

        public bool IsMoving() => lastValue != value;

        internal void Update(float? interpolation = null)
        {
            if (interpolation != null) interpolate = interpolation.Value;
            currentValue = Mathf.Lerp(lastValue, value, InterpolationFramesCount);
            lastValue = currentValue;
        }
    }
}