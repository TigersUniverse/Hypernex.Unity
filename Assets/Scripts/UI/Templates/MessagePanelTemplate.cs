using Hypernex.UIActions.Data;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Templates
{
    public class MessagePanelTemplate : MonoBehaviour
    {
        public MessageUrgency UrgencyPanel;
        public TMP_Text Header;
        public TMP_Text Description;

        public void Render(string h, string d)
        {
            Header.text = h;
            if (Description != null)
                Description.text = d;
            else
                Header.text = d;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}