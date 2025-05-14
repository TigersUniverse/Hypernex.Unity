using System;
using System.Globalization;
using TMPro;
using VRCFaceTracking.Core.Models;

namespace Hypernex.UI.Abstraction
{
    public class UnifiedMutationRender : UIRender, IRender<(MutationConfig, string)>
    {
        public TMP_Text MutationName;
        public TMP_InputField Name;
        public TMP_InputField Cecil;
        public TMP_InputField Floor;
        public TMP_InputField SmoothnessMult;

        private Action action;

        public void AddAction(Action a) => action = a;

        public void Render((MutationConfig, string) item)
        {
            MutationName.text = item.Item2;
            Name.text = item.Item1.Name;
            Cecil.text = item.Item1.Ceil.ToString(CultureInfo.InvariantCulture);
            Floor.text = item.Item1.Floor.ToString(CultureInfo.InvariantCulture);
            SmoothnessMult.text = item.Item1.SmoothnessMult.ToString(CultureInfo.InvariantCulture);
        }

        public void CopyTo(ref MutationConfig unifiedMutation)
        {
            unifiedMutation.Name = string.IsNullOrEmpty(Name.text) ? "" : Name.text;
            try{unifiedMutation.Ceil = Convert.ToSingle(Cecil.text);}catch(Exception){}
            try{unifiedMutation.Floor = Convert.ToSingle(Floor.text);}catch(Exception){}
            try{unifiedMutation.SmoothnessMult = Convert.ToSingle(SmoothnessMult.text);}catch(Exception){}
        }

        public void OnValueChanged()
        {
            try
            {
                action.Invoke();
            }catch(Exception){}
        }
    }
}