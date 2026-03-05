using Hypernex.CCK.Unity.Assets;
using Hypernex.Game;
using Hypernex.Tools;
using Hypernex.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction.AvatarControllers
{
    public class AxisControl : UIAxis, IRender<(AvatarControl, AvatarParameter, AvatarParameter)>, IParameterControl
    {
        public TMP_Text ControlText;
        public Image Icon;

        private AvatarParameter avatarParameter1;
        private AvatarParameter avatarParameter2;
        private Vector2 state;
        
        public void Render((AvatarControl, AvatarParameter, AvatarParameter) t)
        {
            base.Render();
            ControlText.text = t.Item1.ControlName;
            Icon.gameObject.SetActive(t.Item1.ControlSprite != null);
            Icon.sprite = t.Item1.ControlSprite;
            avatarParameter1 = t.Item2;
            avatarParameter2 = t.Item3;
            UpdateState();
        }

        public void UpdateState()
        {
            if (LocalPlayer.Instance.AvatarCreator == null) return;
            float x = LocalPlayer.Instance.AvatarCreator.GetParameter(avatarParameter1.ParameterName).ParameterToFloat();
            float y = LocalPlayer.Instance.AvatarCreator.GetParameter(avatarParameter2.ParameterName).ParameterToFloat();
            state = new Vector2(x, y);
            MoveDotToPosition(state);
        }
        
        protected override void AxisPositionChanged(Vector2 v2)
        {
            LocalPlayer.Instance.AvatarCreator.SetParameter(avatarParameter1.ParameterName, v2.x, null, true);
            LocalPlayer.Instance.AvatarCreator.SetParameter(avatarParameter2.ParameterName, v2.y, null, true);
        }
    }
}