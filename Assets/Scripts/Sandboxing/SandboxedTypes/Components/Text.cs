using System;
using TMPro;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Text
    {
        private readonly bool read;
        private TMP_Text tmpt;

        public Text(Item i)
        {
            read = i.IsReadOnly;
            tmpt = i.t.GetComponent<TMP_Text>();
            if(tmpt == null)
                throw new Exception("No Text found on Item at " + i.Path + ". Are you using TextMeshPro?");
        }
        
        public string GetText()
        {
            if (tmpt != null)
                return tmpt.text;
            return String.Empty;
        }

        public void SetText(string text)
        {
            if(read)
                return;
            if (tmpt != null)
                tmpt.text = text;
        }

        public bool RichText
        {
            get => tmpt != null && tmpt.richText;
            set
            {
                if(read) return;
                tmpt.richText = value;
            }
        }
    }
}