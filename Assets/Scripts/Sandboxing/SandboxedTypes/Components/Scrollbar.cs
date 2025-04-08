using System;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Scrollbar
    {
        private readonly bool read;
        private UnityEngine.UI.Scrollbar scrollbar;

        public Scrollbar(Item i)
        {
            read = i.IsReadOnly;
            scrollbar = i.t.GetComponent<UnityEngine.UI.Scrollbar>();
            if(scrollbar == null)
                throw new Exception("No Scrollbar found on Item at " + i.Path);
        }
        
        public bool Enabled
        {
            get => scrollbar == null ? false : scrollbar.enabled;
            set
            {
                if(read || scrollbar == null) return;
                scrollbar.enabled = value;
            }
        }
        
        public float GetValue()
        {
            if (scrollbar != null)
                return scrollbar.value;
            return float.NaN;
        }
        
        public void SetValue(float value)
        {
            if(read)
                return;
            if (scrollbar != null)
                scrollbar.value = value;
        }

        public void RegisterValueChanged(object o)
        {
            if(read)
                return;
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            if (scrollbar != null)
                scrollbar.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, scrollbar.value));
        }
        
        public void RemoveAllValuesChanged()
        {
            if(read)
                return;
            if (scrollbar != null)
                scrollbar.onValueChanged.RemoveAllListeners();
        }
        
        public ColorBlock? GetColorBlock()
        {
            if (scrollbar != null)
                return ColorBlock.FromUnityColorBlock(scrollbar.colors);
            return null;
        }

        public void SetColorBlock(ColorBlock colorBlock)
        {
            if(read)
                return;
            UnityEngine.UI.ColorBlock unityColorBlock = colorBlock.ToUnityColorBlock();
            if (scrollbar != null)
                scrollbar.colors = unityColorBlock;
        }
    }
}