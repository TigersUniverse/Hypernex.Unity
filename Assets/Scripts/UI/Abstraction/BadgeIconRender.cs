using Hypernex.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex_UI.Scripts.UI.Abstraction
{
    public class BadgeIconRender : UIRender, IRender<Sprite>
    {
        public Image BadgeIcon;
        
        public void Render(Sprite t)
        {
            if (BadgeIcon == null)
            {
                Destroy(gameObject);
                return;
            }

            BadgeIcon.sprite = t;
        }
    }
}