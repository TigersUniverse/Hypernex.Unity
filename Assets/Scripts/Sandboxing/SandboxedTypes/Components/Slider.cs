using System;
using Nexbox;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Slider
    {
        private readonly bool read;
        private UnityEngine.UI.Slider slider;

        public Slider(Item i)
        {
            read = i.IsReadOnly;
            slider = i.t.GetComponent<UnityEngine.UI.Slider>();
            if(slider == null)
                throw new Exception("No Slider found on Item at " + i.Path);
        }
        
        public float GetValue()
        {
            if (slider != null)
                return slider.value;
            return float.NaN;
        }
        
        public void SetValue(float value)
        {
            if(read)
                return;
            if (slider != null)
                slider.value = value;
        }
        
        public void RegisterValueChanged(object o)
        {
            if(read)
                return;
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            if (slider != null)
                slider.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, slider.value));
        }
        
        public void RemoveAllValuesChanged()
        {
            if(read)
                return;
            if (slider != null)
                slider.onValueChanged.RemoveAllListeners();
        }

        public float GetMinimum()
        {
            if (slider != null)
                return slider.minValue;
            return float.NaN;
        }

        public float GetMaximum()
        {
            if (slider != null)
                return slider.maxValue;
            return float.NaN;
        }

        public void SetRange(float minimum, float maximum)
        {
            if(read)
                return;
            if (slider == null) return;
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.value = Mathf.Clamp(slider.value, minimum, maximum);
        }
        
        public ColorBlock? GetColorBlock()
        {
            if (slider != null)
                return ColorBlock.FromUnityColorBlock(slider.colors);
            return null;
        }

        public void SetColorBlock(ColorBlock colorBlock)
        {
            if(read)
                return;
            UnityEngine.UI.ColorBlock unityColorBlock = colorBlock.ToUnityColorBlock();
            if (slider != null)
                slider.colors = unityColorBlock;
        }
    }
}