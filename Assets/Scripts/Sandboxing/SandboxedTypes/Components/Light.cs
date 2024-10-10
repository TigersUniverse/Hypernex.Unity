using System;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Light
    {
        private readonly Item item;
        private readonly bool read;
        private UnityEngine.Light light;
        
        private static UnityEngine.Light GetLight(Item item)
        {
            UnityEngine.Light a = item.t.GetComponent<UnityEngine.Light>();
            if (a == null)
                return null;
            return a;
        }

        public Light(Item i)
        {
            item = i;
            read = i.IsReadOnly;
            light = GetLight(i);
            if (light == null) throw new Exception("No Light found on Item at " + i.Path);
        }

        public bool IsValid() => light != null;

        public bool IsEnabled
        {
            get => light.enabled;
            set
            {
                if (read || light == null)
                    return;
                light.enabled = value;
            }
        }

        public float Range
        {
            get => light.range;
            set
            {
                if (read || light == null)
                    return;
                light.range = value;
            }
        }

        public float Intensity
        {
            get => light.intensity;
            set
            {
                if (read || light == null)
                    return;
                light.intensity = value;
            }
        }

        public Color Color
        {
            get => Color.FromUnityColor(light.color);
            set
            {
                if (read || light == null)
                    return;
                light.color = value.ToUnityColor();
            }
        }

        public LightShadows Shadows
        {
            get => light.shadows;
            set
            {
                if (read || light == null)
                    return;
                light.shadows = value;
            }
        }
    }
}