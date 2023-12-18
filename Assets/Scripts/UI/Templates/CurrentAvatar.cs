using System;
using System.Collections.Generic;
using System.Linq;
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
        public static CurrentAvatar Instance { get; internal set; }
        
        public LoginPageTopBarButton CurrentAvatarPage;
        public DashboardManager DashboardManager;
        public TMP_Text AvatarNameLabel;
        public TMP_Text AvatarScaleLabel;
        public Slider AvatarScaleSlider;
        public DynamicScroll ParameterButtons;
        public List<ParameterTemplate> ParameterTemplates = new();

        public void Render(AvatarCreator avatarCreator)
        {
            CurrentAvatarPage.Show();
            ParameterButtons.Clear();
            AvatarNameLabel.text = "Current Avatar: " + LocalPlayer.Instance.avatarMeta.Name;
            foreach (AnimatorPlayable animatorPlayable in avatarCreator.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter animatorControllerParameter in animatorPlayable
                             .AnimatorControllerParameters)
                {
                    if (avatarCreator.Avatar.ShowAllParameters ||
                        avatarCreator.Avatar.VisibleParameters.Contains(animatorControllerParameter.name))
                        CreateParameterButton(animatorPlayable, animatorControllerParameter.name,
                            animatorControllerParameter.type);
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

        private void CreateParameterButton(AnimatorPlayable animatorPlayable, string parameterName,
            AnimatorControllerParameterType t)
        {
            GameObject parameterButton = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("ParameterSelect").gameObject;
            GameObject newParameterButton = Instantiate(parameterButton);
            Button b = newParameterButton.GetComponent<Button>();
            b.onClick.AddListener(() =>
            {
                ParameterTemplates.ForEach(x => x.gameObject.SetActive(false));
                ParameterTemplate pt = ParameterTemplates.First(x => x.ParameterType == t);
                pt.Render(animatorPlayable, parameterName);
                pt.gameObject.SetActive(true);
            });
            newParameterButton.transform.GetChild(0).GetComponent<TMP_Text>().text = parameterName;
            RectTransform c = newParameterButton.GetComponent<RectTransform>();
            ParameterButtons.AddItem(c);
        }

        private void Update()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null ||
                !LocalPlayer.Instance.avatar.calibrated || (GameInstance.FocusedInstance != null &&
                                                            GameInstance.FocusedInstance.World != null &&
                                                            !GameInstance.FocusedInstance.World.AllowScaling))
            {
                AvatarScaleSlider.gameObject.SetActive(false);
                return;
            }
            AvatarScaleSlider.gameObject.SetActive(true);
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