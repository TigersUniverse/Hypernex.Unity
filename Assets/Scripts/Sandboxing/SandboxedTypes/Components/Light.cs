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
        
        public bool Enabled
        {
            get => light == null ? false : light.enabled;
            set
            {
                if(read || light == null) return;
                light.enabled = value;
            }
        }

        public Light(Item i)
        {
            item = i;
            read = i.IsReadOnly;
            light = GetLight(i);
            if (light == null) throw new Exception("No Light found on Item at " + i.Path);
        }

        public LightType Type
        {
            get => light.type;
            set
            {
                if (read || light == null)
                    return;
                light.type = value;
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

        public float SpotAngle
        {
            get => light.spotAngle;
            set
            {
                if (read || light == null)
                    return;
                light.spotAngle = value;
            }
        }

        public float InnerSpotAngle
        {
            get => light.innerSpotAngle;
            set
            {
                if (read || light == null)
                    return;
                light.innerSpotAngle = value;
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

        public float ColorTemperature
        {
            get => light.colorTemperature;
            set
            {
                if (read || light == null)
                    return;
                light.colorTemperature = value;
            }
        }

        public bool UseColorTemperature
        {
            get => light.useColorTemperature;
            set
            {
                if (read || light == null)
                    return;
                light.useColorTemperature = value;
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

        public float ShadowStrength
        {
            get => light.shadowStrength;
            set
            {
                if (read || light == null)
                    return;
                light.shadowStrength = value;
            }
        }
    }
}