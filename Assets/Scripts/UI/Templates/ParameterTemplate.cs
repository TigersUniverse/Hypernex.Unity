using System;
using System.Globalization;
using Hypernex.CCK.Unity.Internals;
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
        private AnimatorControllerParameterType literal;

        public void Apply()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null || AnimatorPlayable == null)
                return;
            switch (ParameterType)
            {
                case AnimatorControllerParameterType.Bool:
                    switch (literal)
                    {
                        case AnimatorControllerParameterType.Bool:
                        {
                            bool lastValue = LocalPlayer.Instance.avatar.GetParameter<bool>(ParameterName,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            LocalPlayer.Instance.avatar.SetParameter(ParameterName, !lastValue,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            break;
                        }
                        case AnimatorControllerParameterType.Int:
                        {
                            int lastValue = LocalPlayer.Instance.avatar.GetParameter<int>(ParameterName,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            if (lastValue == 1) lastValue = 0;
                            else if (lastValue == 0) lastValue = 1;
                            LocalPlayer.Instance.avatar.SetParameter(ParameterName, lastValue,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            break;
                        }
                        case AnimatorControllerParameterType.Float:
                        {
                            float lastValue = LocalPlayer.Instance.avatar.GetParameter<float>(ParameterName,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            if (lastValue == 1f) lastValue = 0f;
                            else if (lastValue == 0f) lastValue = 1f;
                            LocalPlayer.Instance.avatar.SetParameter(ParameterName, lastValue,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            break;
                        }
                    }
                    break;
                case AnimatorControllerParameterType.Int:
                    switch (literal)
                    {
                        case AnimatorControllerParameterType.Bool:
                        {
                            int intValue = (int) Convert.ChangeType(ParameterValueInput.text, typeof(int));
                            LocalPlayer.Instance.avatar.SetParameter(ParameterName, intValue > 0,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            break;
                        }
                        case AnimatorControllerParameterType.Int:
                        {
                            int intValue = (int) Convert.ChangeType(ParameterValueInput.text, typeof(int));
                            LocalPlayer.Instance.avatar.SetParameter(ParameterName, intValue,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            break;
                        }
                        case AnimatorControllerParameterType.Float:
                        {
                            int intValue = (int) Convert.ChangeType(ParameterValueInput.text, typeof(int));
                            LocalPlayer.Instance.avatar.SetParameter(ParameterName, (float) intValue,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            break;
                        }
                    }
                    break;
                case AnimatorControllerParameterType.Float:
                    switch (literal)
                    {
                        case AnimatorControllerParameterType.Bool:
                        {
                            float floatValue = (float) Math.Round(ParameterValueSlider.value, 2);
                            LocalPlayer.Instance.avatar.SetParameter(ParameterName, floatValue > 0,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            break;
                        }
                        case AnimatorControllerParameterType.Int:
                        {
                            float floatValue = (float) Math.Round(ParameterValueSlider.value, 2);
                            LocalPlayer.Instance.avatar.SetParameter(ParameterName, (int) floatValue,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            break;
                        }
                        case AnimatorControllerParameterType.Float:
                        {
                            float floatValue = (float) Math.Round(ParameterValueSlider.value, 2);
                            LocalPlayer.Instance.avatar.SetParameter(ParameterName, floatValue,
                                AnimatorPlayable.Value.CustomPlayableAnimator);
                            break;
                        }
                    }
                    break;
            }
        }

        public void Render(AnimatorPlayable animatorPlayable, string parameterName, AnimatorControllerParameterType l)
        {
            AnimatorPlayable = animatorPlayable;
            ParameterName = parameterName;
            ParameterNameLabel.text = ParameterName;
            literal = l;
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