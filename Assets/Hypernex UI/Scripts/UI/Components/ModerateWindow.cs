using System;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Components
{
    public class ModerateWindow : MonoBehaviour
    {
        public TMP_Text ActionReceiver;
        public TMP_InputField Message;
        public TMP_Text ButtonText;
        
        private Action<string> OnSubmit;
        
        public void Apply(string action, string receiver, Action<string> callback)
        {
            ActionReceiver.text = $"You are about to {action} {receiver}";
            ButtonText.text = $"{action} {receiver}";
            Message.text = String.Empty;
            OnSubmit = callback;
            gameObject.SetActive(true);
        }
        
        public void OnSubmitButtonClick()
        {
            OnSubmit.Invoke(Message.text);
            Return();
        }

        public void Return() => gameObject.SetActive(false);
    }
}