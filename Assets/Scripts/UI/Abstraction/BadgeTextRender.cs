using Hypernex.UI;
using TMPro;

namespace Hypernex_UI.Scripts.UI.Abstraction
{
    public class BadgeTextRender : UIRender, IRender<string>
    {
        public TMP_Text BadgeText;
        
        public void Render(string t)
        {
            if (BadgeText == null)
            {
                Destroy(gameObject);
                return;
            }
            BadgeText.text = t;
        }
    }
}