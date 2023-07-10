using System;
using System.Globalization;
using Hypernex.CCK.Unity;
using Hypernex.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class ParameterTemplate : MonoBehaviour
    {
        public AnimatorControllerParameterType ParameterType;
        public TMP_Text ParameterNameLabel;
        public TMP_Text ParameterValueLabel;
        public Slider ParameterValueSlider;
        public TMP_Text ParameterSliderText;
        public TMP_InputField ParameterValueInput;
        
        private AnimatorPlayable? AnimatorPlayable;
        private string ParameterName;

        public void Apply()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null || AnimatorPlayable == null)
                return;
            switch (ParameterType)
            {
                case AnimatorControllerParameterType.Bool:
                    bool lastValue = LocalPlayer.Instance.avatar.GetParameter<bool>(ParameterName,
                        AnimatorPlayable.Value.CustomPlayableAnimator);
                    LocalPlayer.Instance.avatar.SetParameter(ParameterName, !lastValue,
                        AnimatorPlayable.Value.CustomPlayableAnimator);
                    break;
                case AnimatorControllerParameterType.Int:
                    int intValue = (int) Convert.ChangeType(ParameterValueInput.text, typeof(int));
                    LocalPlayer.Instance.avatar.SetParameter(ParameterName, intValue,
                        AnimatorPlayable.Value.CustomPlayableAnimator);
                    break;
                case AnimatorControllerParameterType.Float:
                    LocalPlayer.Instance.avatar.SetParameter(ParameterName, ParameterValueSlider.value,
                        AnimatorPlayable.Value.CustomPlayableAnimator);
                    break;
            }
        }

        public void Render(AnimatorPlayable animatorPlayable, string parameterName)
        {
            AnimatorPlayable = animatorPlayable;
            ParameterName = parameterName;
            ParameterNameLabel.text = ParameterName;
            if (ParameterType == AnimatorControllerParameterType.Float)
                ParameterValueSlider.value = LocalPlayer.Instance.avatar.GetParameter<float>(ParameterName,
                    AnimatorPlayable.Value.CustomPlayableAnimator);
        }

        private void Update()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null || AnimatorPlayable == null)
                return;
            ParameterValueLabel.text = "Parameter Value: " +
                                       LocalPlayer.Instance.avatar.GetParameter(ParameterName,
                                           AnimatorPlayable.Value.CustomPlayableAnimator);
            if (ParameterValueSlider != null && ParameterSliderText != null)
                ParameterSliderText.text = Math.Round(ParameterValueSlider.value, 2)
                    .ToString(CultureInfo.InvariantCulture);
        }
    }
}