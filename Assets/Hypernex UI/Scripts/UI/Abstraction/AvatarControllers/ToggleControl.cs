using Hypernex.CCK.Unity.Assets;
using Hypernex.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction.AvatarControllers
{
    public class ToggleControl : MonoBehaviour, IRender<(AvatarControl,AvatarParameter)>, IParameterControl
    {
        private const string ANIMATOR_PARAMETER = "EnabledThing";
        
        public Animator Animator;
        public TMP_Text ControlText;
        public Image Icon;

        private AvatarParameter avatarParameter1;
        private bool state;
        
        public void Render((AvatarControl, AvatarParameter) t)
        {
            ControlText.text = t.Item1.ControlName;
            if(t.Item1.ControlSprite != null) Icon.sprite = t.Item1.ControlSprite;
            avatarParameter1 = t.Item2;
            UpdateState();
        }
        
        public void UpdateState()
        {
            if (LocalPlayer.Instance.AvatarCreator == null) return;
            state = LocalPlayer.Instance.AvatarCreator.GetParameter<bool>(avatarParameter1.ParameterName);
            SetAnimatorState();
        }

        public void Toggle()
        {
            if(avatarParameter1 == null) return;
            state = !state;
            LocalPlayer.Instance.AvatarCreator.SetParameter(avatarParameter1.ParameterName, state);
            SetAnimatorState();
        }

        private void SetAnimatorState()
        {
            if(Animator == null) return;
            Animator.SetBool(ANIMATOR_PARAMETER, state);
        }
    }
}