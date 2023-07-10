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

        public void OnAvatarSlider()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            float lv = LocalPlayer.Instance.transform.localScale.y;
            float v = (float) Math.Round(AvatarScaleSlider.value, 1);
            LocalPlayer.Instance.transform.localScale = new Vector3(v, v, v);
            Vector3 lp = LocalPlayer.Instance.transform.position;
            LocalPlayer.Instance.transform.position = new Vector3(lp.x, lp.y + (v - lv), lp.z);
            LocalPlayer.Instance.Dashboard.PositionDashboard(LocalPlayer.Instance);
        }

        public void RefreshAvatar()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            LocalPlayer.Instance.RefreshAvatar();
            LoginPageTopBarButton.Show("Home");
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
            AvatarScaleLabel.text = "Avatar Scale: " + Math.Round(LocalPlayer.Instance.transform.localScale.y, 1);
        }
    }
}