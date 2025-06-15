using TMPro;

namespace Hypernex.UI.Abstraction
{
    public class NotificationRender : UIRender, IRender<(string, string)>
    {
        public MessageUrgency UrgencyPanel;
        public TMP_Text Header;
        public TMP_Text Description;
        
        public void Render((string, string) t)
        {
            Header.text = t.Item1;
            if (Description != null)
                Description.text = t.Item2;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}