using Hypernex.CCK.Unity.Assets;
using UnityEngine;

namespace Hypernex.UI.Abstraction
{
    public class AvatarControlRender : UIRender, IRender<AvatarControl>
    {
        private const string PARAMETER_NAME = "AC_State";
        public Animator TargetAnimator;
        
        public void Render(AvatarControl t)
        {
            switch (t.ControlType)
            {
                case ControlType.Toggle:
                    TargetAnimator.SetInteger(PARAMETER_NAME, 1);
                    break;
                case ControlType.Slider:
                    TargetAnimator.SetInteger(PARAMETER_NAME, 2);
                    break;
                case ControlType.Dropdown:
                    TargetAnimator.SetInteger(PARAMETER_NAME, 3);
                    break;
                case ControlType.TwoDimensionalAxis:
                    TargetAnimator.SetInteger(PARAMETER_NAME, 4);
                    break;
            }
        }
    }
}