using System;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Toggle
    {
        private readonly bool read;
        private UnityEngine.UI.Toggle t;
        
        public Toggle(Item i)
        {
            read = i.IsReadOnly;
            t = i.t.GetComponent<UnityEngine.UI.Toggle>();
            if(t == null)
                throw new Exception("No Toggle found on Item at " + i.Path);
        }
        
        public bool Enabled
        {
            get => t == null ? false : t.enabled;
            set
            {
                if(read || t == null) return;
                t.enabled = value;
            }
        }
        
        public bool GetToggle()
        {
            if (t != null)
                return t.isOn;
            return false;
        }
        
        public void RegisterValueChanged(object o)
        {
            if(read)
                return;
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            if (t != null)
                t.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, t.isOn));
        }
        
        public void RemoveAllValuesChanged()
        {
            if(read)
                return;
            if (t != null)
                t.onValueChanged.RemoveAllListeners();
        }

        public void SetToggle(bool value)
        {
            if(read)
                return;
            if (t != null)
                t.isOn = value;
        }
        
        public ColorBlock? GetColorBlock()
        {
            if (t != null)
                return ColorBlock.FromUnityColorBlock(t.colors);
            return null;
        }

        public void SetColorBlock(ColorBlock colorBlock)
        {
            if(read)
                return;
            UnityEngine.UI.ColorBlock unityColorBlock = colorBlock.ToUnityColorBlock();
            if (t != null)
                t.colors = unityColorBlock;
        }
    }
}