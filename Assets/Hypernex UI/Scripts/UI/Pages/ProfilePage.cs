using Hypernex.UI.Abstraction;
using HypernexSharp.APIObjects;
using UnityEngine;

namespace Hypernex.UI.Pages
{
    [RequireComponent(typeof(UserRender))]
    public class ProfilePage : UIPage
    {
        public User UserToRender;

        private UserRender userRender;

        public override void Show(bool hideAll = true)
        {
            base.Show(hideAll);
            if(UserToRender == null) return;
            userRender.Render(UserToRender);
        }

        private void Start() => userRender = GetComponent<UserRender>();
    }
}