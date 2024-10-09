using System;
using Nexbox;
using TMPro;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Dropdown
    {
        private readonly bool read;
        private TMP_Dropdown tdd;

        public Dropdown(Item i)
        {
            read = i.IsReadOnly;
            tdd = i.t.GetComponent<TMP_Dropdown>();
            if(tdd == null)
                throw new Exception("No Dropdown found on Item at " + i.Path + ". Are you using TextMeshPro?");
        }
        
        public int GetValue()
        {
            if (tdd != null)
                return tdd.value;
            return -1;
        }
        
        public void SetValue(int value)
        {
            if(read)
                return;
            if (tdd != null)
                tdd.value = value;
        }

        public void RegisterValueChanged(object o)
        {
            if(read)
                return;
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            if (tdd != null)
                tdd.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, tdd.value));
        }
        
        public void RemoveAllValuesChanged()
        {
            if(read)
                return;
            if (tdd != null)
                tdd.onValueChanged.RemoveAllListeners();
        }
        
        public ColorBlock? GetColorBlock()
        {
            if(tdd != null)
                return ColorBlock.FromUnityColorBlock(tdd.colors);
            return null;
        }

        public void SetColorBlock(ColorBlock colorBlock)
        {
            UnityEngine.UI.ColorBlock unityColorBlock = colorBlock.ToUnityColorBlock();
            if(tdd != null)
                tdd.colors = unityColorBlock;
        }
    }
}