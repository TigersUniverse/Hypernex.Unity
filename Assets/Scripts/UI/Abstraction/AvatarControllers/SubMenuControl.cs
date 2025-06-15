using Hypernex.CCK.Unity.Assets;
using Hypernex.UI.Renderer;
using TMPro;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction.AvatarControllers
{
    public class SubMenuControl : UIRender, IRender<(AvatarOptionsRenderer, AvatarControl)>
    {
        public TMP_Text ControlText;
        public Image Icon;

        private AvatarOptionsRenderer avatarOptionsRenderer;
        private AvatarMenu subMenu;
        
        public void Render((AvatarOptionsRenderer, AvatarControl) t)
        {
            avatarOptionsRenderer = t.Item1;
            ControlText.text = t.Item2.ControlName;
            if(t.Item2.ControlSprite != null) Icon.sprite = t.Item2.ControlSprite;
            subMenu = t.Item2.SubMenu;
        }

        public void OnSelect() => avatarOptionsRenderer.ShowMenu(subMenu, false);
    }
}