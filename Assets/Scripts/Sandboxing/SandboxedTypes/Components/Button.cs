using System;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Button
    {
        private readonly bool read;
        private UnityEngine.UI.Button b;

        public Button(Item i)
        {
            read = i.IsReadOnly;
            b = i.t.GetComponent<UnityEngine.UI.Button>();
            if(b == null)
                throw new Exception("No Button found on Item at " + i.Path);
        }
        
        public void RegisterClick(object o)
        {
            if(read)
                return;
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            if(b != null)
                b.onClick.AddListener(() => SandboxFuncTools.InvokeSandboxFunc(s));
        }

        public void RemoveAllClicks()
        {
            if(read)
                return;
            if(b != null)
                b.onClick.RemoveAllListeners();
        }
        
        public ColorBlock? GetColorBlock()
        {
            if (b != null)
                return ColorBlock.FromUnityColorBlock(b.colors);
            return null;
        }
        
        public void SetColorBlock(ColorBlock colorBlock)
        {
            if(read)
                return;
            UnityEngine.UI.ColorBlock unityColorBlock = colorBlock.ToUnityColorBlock();
            if (b != null)
                b.colors = unityColorBlock;
        }
    }
}