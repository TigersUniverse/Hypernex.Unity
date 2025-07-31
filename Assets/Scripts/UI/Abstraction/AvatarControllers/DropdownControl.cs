using Hypernex.CCK.Unity.Assets;
using Hypernex.Game;
using Hypernex.Tools;
using Hypernex.UI.Components;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Abstraction.AvatarControllers
{
    [RequireComponent(typeof(ToggleButton))]
    public class DropdownControl : UIRender, IRender<(AvatarControl, AvatarParameter)>, IParameterControl
    {
        public TMP_Text LabelText;
        public ToggleButton ToggleButton;
        
        private AvatarParameter avatarParameter1;
        private int index;
        private int state;
        
        public void Render((AvatarControl, AvatarParameter) t)
        {
            index = transform.GetSiblingIndex();
            LabelText.text = t.Item1.DropdownOptions[index];
            avatarParameter1 = t.Item2;
            UpdateState();
        }
        
        public void UpdateState()
        {
            if (LocalPlayer.Instance.AvatarCreator == null) return;
            state = LocalPlayer.Instance.AvatarCreator.GetParameter(avatarParameter1.ParameterName).ParameterToInt();
            if(state == index) ToggleButton.Select();
        }

        public void OnSelect()
        {
            state = index;
            LocalPlayer.Instance.AvatarCreator.SetParameter(avatarParameter1.ParameterName, state, null, true);
        }
    }
}