using System;
using Hypernex.CCK.Unity;
using Hypernex.Game;
using Hypernex.UIActions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class CurrentAvatar : MonoBehaviour
    {
        public LoginPageTopBarButton CurrentAvatarPage;
        public DashboardManager DashboardManager;
        public TMP_Text AvatarNameLabel;
        public TMP_Text AvatarScaleLabel;
        public Slider AvatarScaleSlider;
        public DynamicScroll Parameters;

        public void Render(AvatarCreator avatarCreator)
        {
            CurrentAvatarPage.Show();
            Parameters.Clear();
            AvatarNameLabel.text = "Current Avatar: " + LocalPlayer.Instance.avatarMeta.Name;
            foreach (AnimatorPlayable animatorPlayable in avatarCreator.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter animatorControllerParameter in animatorPlayable
                             .AnimatorControllerParameters)
                {
                    switch (animatorControllerParameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            CreateBoolParameterBox(animatorPlayable, animatorControllerParameter.name);
                            break;
                        case AnimatorControllerParameterType.Int:
                            CreateIntParameterBox(animatorPlayable, animatorControllerParameter.name);
                            break;
                        case AnimatorControllerParameterType.Float:
                            CreateFloatParameterBox(animatorPlayable, animatorControllerParameter.name);
                            break;
                    }
                }
            }
        }

        private bool IsVRTriggerPressed()
        {
            foreach (IBinding binding in LocalPlayer.Instance.Bindings)
            {
                if (binding.Trigger > 0.9f)
                    return true;
            }
            return false;
        }

        public void RefreshAvatar(bool lpi)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            SizeAvatar(1f);
            if(lpi)
                LocalPlayer.Instance.RefreshAvatar(true);
            AvatarScaleSlider.value = LocalPlayer.Instance.transform.localScale.y;
            if(lpi)
                LoginPageTopBarButton.Show("Home");
        }

        public void SizeAvatar(float v)
        {
            AvatarScaleSlider.value = v;
            LocalPlayer.Instance.transform.localScale = new Vector3(v, v, v);
            Vector3 lp = LocalPlayer.Instance.transform.position;
            float scaleUp = DashboardManager.OpenedPosition.y + (v - DashboardManager.OpenedScale.y);
            float scaleDown = DashboardManager.OpenedBounds.min.y + v/2;
            LocalPlayer.Instance.transform.position = new Vector3(lp.x, v >= DashboardManager.OpenedScale.y ? scaleUp : scaleDown, lp.z);
            LocalPlayer.Instance.Dashboard.PositionDashboard(LocalPlayer.Instance);
        }
        
        private void CreateIntParameterBox(AnimatorPlayable animatorPlayable, string parameterName)
        {
            GameObject instanceCard = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("IntParameter").gameObject;
            GameObject newInstanceCard = Instantiate(instanceCard);
            RectTransform c = newInstanceCard.GetComponent<RectTransform>();
            newInstanceCard.GetComponent<ParameterTemplate>().Render(animatorPlayable, parameterName);
            Parameters.AddItem(c);
        }
        
        private void CreateFloatParameterBox(AnimatorPlayable animatorPlayable, string parameterName)
        {
            GameObject instanceCard = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("FloatParameter").gameObject;
            GameObject newInstanceCard = Instantiate(instanceCard);
            RectTransform c = newInstanceCard.GetComponent<RectTransform>();
            newInstanceCard.GetComponent<ParameterTemplate>().Render(animatorPlayable, parameterName);
            Parameters.AddItem(c);
        }
        
        private void CreateBoolParameterBox(AnimatorPlayable animatorPlayable, string parameterName)
        {
            GameObject instanceCard = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("BoolParameter").gameObject;
            GameObject newInstanceCard = Instantiate(instanceCard);
            RectTransform c = newInstanceCard.GetComponent<RectTransform>();
            newInstanceCard.GetComponent<ParameterTemplate>().Render(animatorPlayable, parameterName);
            Parameters.AddItem(c);
        }

        private void Update()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            // Doesn't matter which dimension, they should always be uniform
            AvatarScaleLabel.text = "Avatar Scale: " + Math.Round(AvatarScaleSlider.value, 1);
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            float lv = LocalPlayer.Instance.transform.localScale.y;
            float v = (float) Math.Round(AvatarScaleSlider.value, 1);
            if(!LocalPlayer.IsVR || !IsVRTriggerPressed() && v != (float) Math.Round(lv, 1))
                SizeAvatar(v);
        }
    }
}