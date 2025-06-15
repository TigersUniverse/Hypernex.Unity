using Hypernex.UI.Renderer;

namespace Hypernex.UI.Abstraction.AvatarControllers
{
    public class ReturnButtonControl : UIRender, IRender<AvatarOptionsRenderer>
    {
        private AvatarOptionsRenderer avatarOptionsRenderer;
        
        public void Render(AvatarOptionsRenderer t) => avatarOptionsRenderer = t;

        public void Return() => avatarOptionsRenderer.Return();
    }
}