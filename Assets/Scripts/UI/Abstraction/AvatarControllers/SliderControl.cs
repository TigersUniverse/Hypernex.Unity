using Hypernex.CCK.Unity.Assets;
using Hypernex.Game;
using Hypernex.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction.AvatarControllers
{
    public class SliderControl : UIRender, IRender<(AvatarControl,AvatarParameter)>, IParameterControl
    {
        public Slider Slider;
        public TMP_Text ControlText;
        public Image Icon;

        private AvatarParameter avatarParameter1;
        private float state;
        
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
            state = LocalPlayer.Instance.AvatarCreator.GetParameter(avatarParameter1.ParameterName).ParameterToFloat();
            Slider.value = state;
        }

        public void OnSliderValueChanged(float v)
        {
            if(avatarParameter1 == null) return;
            state = v;
            LocalPlayer.Instance.AvatarCreator.SetParameter(avatarParameter1.ParameterName, state, null, true);
        }
    }
}