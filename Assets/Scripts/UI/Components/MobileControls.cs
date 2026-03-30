using System;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Components
{
    public class MobileControls : UIRender
    {
        public static MobileControls Instance { get; private set; }

        public Vector2 Move => MoveAxis.GetPosition();

        public UIAxis MoveAxis;
        public ButtonHeld RightClickButtonHeld;
        public ButtonHeld RunButtonHeld;
        public ButtonHeld JumpButtonHeld;
        public ButtonHeld MenuButtonHeld;
        
        private int avatarCrawlState;
        private Vector3 initPosition;

        public bool LeftClickDown => Input.GetMouseButton(0);
        public Action LeftClick = () => { };
        public bool RightClickDown => RightClickButtonHeld.IsHeld;
        public Action RightClick = () => { };
        public bool RunButton => RunButtonHeld.IsHeld;
        public Action OnRunButton = () => { };
        public bool JumpButton => JumpButtonHeld.IsHeld;
        public Action OnJumpButton = () => { };
        public bool MenuButton => MenuButtonHeld.IsHeld; 
        public Action OnMenuButton = () => { };

        public void RightClickCallback() => RightClick.Invoke();
        public void RunClickCallback() => OnRunButton.Invoke();
        public void JumpClickCallback() => OnJumpButton.Invoke();
        public void MenuClickCallback() => OnMenuButton.Invoke();
        
        public void OnCrawl()
        {
            LocalAvatarCreator avatar = LocalPlayer.Instance.avatar;
            if(avatar == null) return;
            ++avatarCrawlState;
            if (avatarCrawlState > 2) avatarCrawlState = 0;
            switch (avatarCrawlState)
            {
                case 0:
                    avatar.SetCrouch(false);
                    avatar.SetCrawl(false);
                    break;
                case 1:
                    avatar.SetCrawl(false);
                    avatar.SetCrouch(true);
                    break;
                case 2:
                    avatar.SetCrouch(false);
                    avatar.SetCrawl(true);
                    break;
            }
        }

        private void Start()
        {
            if (Instance != null)
            {
                Logger.CurrentLogger.Error("Cannot have multiple instances of MobileControls!");
                Destroy(gameObject);
                return;
            }
            initPosition = MoveAxis.Dot.localPosition;
            MoveAxis.Render();
            Instance = this;
        }

        private void Update()
        {
            if(Input.GetMouseButtonDown(0)) LeftClick.Invoke();
            if (!LeftClickDown) MoveAxis.Dot.localPosition = initPosition;
        }
    }
}