using Hypernex.CCK.Unity.Assets;
using Hypernex.Game;
using Hypernex.UI.Components;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Abstraction.AvatarControllers
{
    [RequireComponent(typeof(ToggleButton))]
    public class DropdownControl : MonoBehaviour, IRender<(AvatarControl, AvatarParameter)>, IParameterControl
    {
        public TMP_Text LabelText;
        
        internal ToggleButton ToggleButton;
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
            state = LocalPlayer.Instance.AvatarCreator.GetParameter<int>(avatarParameter1.ParameterName);
            if(state == index) ToggleButton.Select();
        }

        public void OnSelect()
        {
            state = index;
            LocalPlayer.Instance.AvatarCreator.SetParameter(avatarParameter1.ParameterName, state);
        }

        private void Start()
        {
            ToggleButton = GetComponent<ToggleButton>();
            ToggleButton.OnChange.AddListener(OnSelect);
        }
    }
}