using Hypernex.CCK.Unity.Assets;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.UI.Pages
{
    public class AvatarOptionsPage : UIPage
    {
        public Animator Animator;
        public RectTransform CubeHolder;
        
        public void Render(AvatarMenu menu, AvatarMenu previousPage = null)
        {
            CubeHolder.ClearChildren();
            // TODO: implement
        }
    }
}