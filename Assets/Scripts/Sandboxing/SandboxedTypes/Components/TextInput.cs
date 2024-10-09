using System;
using Hypernex.Game;
using Hypernex.UI.Templates;
using Nexbox;
using TMPro;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class TextInput
    {
        private readonly bool read;
        private TMP_InputField tif;
        private TMP_Text placeholderText;

        public TextInput(Item i)
        {
            read = i.IsReadOnly;
            tif = i.t.GetComponent<TMP_InputField>();
            if(tif == null)
                throw new Exception("No TextInput found on Item at " + i.Path + ". Are you using TextMeshPro?");
            placeholderText = tif.placeholder.GetComponent<TMP_Text>();
        }
        
        public void RegisterInputFieldVR()
        {
            if(read)
                return;
            tif.onSelect.RemoveAllListeners();
            tif.onSelect.AddListener(_ =>
            {
                if (!LocalPlayer.IsVR)
                    return;
                KeyboardTemplate.GetKeyboardTemplateByLanguage("en").RequestInput(s => tif.text = s);
            });
        }
        
        public string GetText()
        {
            if (tif != null)
                return tif.text;
            return String.Empty;
        }
        
        public void SetText(string value)
        {
            if(read)
                return;
            if (tif != null)
                tif.text = value;
        }

        public string GetPlaceholderText()
        {
            if (placeholderText != null)
                return placeholderText.text;
            return String.Empty;
        }
        
        public void SetPlaceholderText(string value)
        {
            if(read)
                return;
            if (placeholderText != null)
                placeholderText.text = value;
        }

        public void RegisterTextChanged(object o)
        {
            if(read)
                return;
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            if (tif != null)
                tif.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, tif.text));
        }
        
        public void RemoveAllTextChanged()
        {
            if(read)
                return;
            if (tif != null)
                tif.onValueChanged.RemoveAllListeners();
        }
        
        public bool RichText
        {
            get => tif.richText;
            set
            {
                if(read) return;
                tif.richText = value;
            }
        }
        
        public bool PlaceholderRichText
        {
            get => placeholderText != null && placeholderText.richText;
            set
            {
                if(read || placeholderText == null) return;
                placeholderText.richText = value;
            }
        }
        
        public ColorBlock? GetColorBlock()
        {
            if (tif != null)
                return ColorBlock.FromUnityColorBlock(tif.colors);
            return null;
        }
        
        public void SetColorBlock(ColorBlock colorBlock)
        {
            UnityEngine.UI.ColorBlock unityColorBlock = colorBlock.ToUnityColorBlock();
            if (tif != null)
                tif.colors = unityColorBlock;
        }
    }
}