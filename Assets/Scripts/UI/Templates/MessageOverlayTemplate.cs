using System;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Templates
{
    public class MessageOverlayTemplate : MonoBehaviour
    {
        public TMP_Text Action;
        public TMP_Text ActionReceiver;
        public TMP_InputField Message;
        public TMP_Text ButtonText;

        private Action<string> OnSubmit;

        public void OnSubmitButtonClick()
        {
            OnSubmit.Invoke(Message.text);
            gameObject.SetActive(false);
        }

        public void OnCloseButtonClick() => gameObject.SetActive(false);

        public void Render(string action, string receiver, Action<string> callback)
        {
            Action.text = action;
            ActionReceiver.text = $"You are about to {action} {receiver}";
            ButtonText.text = $"{action} {receiver}";
            OnSubmit = callback;
            gameObject.SetActive(true);
        }
    }
}