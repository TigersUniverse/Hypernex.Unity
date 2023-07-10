using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRCFaceTracking.Core.Models;

namespace Hypernex.UI.Templates
{
    public class UnifiedMutationPanel : MonoBehaviour
    {
        public TMP_Text MutationName;
        public TMP_InputField Name;
        public TMP_InputField Cecil;
        public TMP_InputField Floor;
        public TMP_InputField SmoothnessMult;
        public Button ApplyButton;

        public void AddAction(Action a)
        {
            ApplyButton.onClick.RemoveAllListeners();
            ApplyButton.onClick.AddListener(a.Invoke);
        }

        public void Render(UnifiedMutation unifiedMutation, string n)
        {
            MutationName.text = n;
            Name.text = unifiedMutation.Name;
            Cecil.text = unifiedMutation.Ceil.ToString(CultureInfo.InvariantCulture);
            Floor.text = unifiedMutation.Floor.ToString(CultureInfo.InvariantCulture);
            SmoothnessMult.text = unifiedMutation.SmoothnessMult.ToString(CultureInfo.InvariantCulture);
        }

        public void CopyTo(ref UnifiedMutation unifiedMutation)
        {
            unifiedMutation.Name = string.IsNullOrEmpty(Name.text) ? null : Name.text;
            try{unifiedMutation.Ceil = Convert.ToSingle(Cecil.text);}catch(Exception){}
            try{unifiedMutation.Floor = Convert.ToSingle(Floor.text);}catch(Exception){}
            try{unifiedMutation.SmoothnessMult = Convert.ToSingle(SmoothnessMult.text);}catch(Exception){}
        }
    }
}