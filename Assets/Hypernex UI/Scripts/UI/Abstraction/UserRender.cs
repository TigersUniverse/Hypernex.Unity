using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction
{
    public abstract class UserRender : MonoBehaviour, IUIRenderer<User>
    {
        public RawImage ProfileIcon;
        public RawImage ProfileBanner;
        public Image StatusIcon;
        public TMP_Text Username;
        public TMP_Text StatusText;
        public TMP_Text DescriptionText;
        public PronounRender PronounRender;

        public void Render(User user)
        {
            if (Username != null)
                Username.text = user.GetUserDisplayName();
            if(StatusText != null)
                StatusText.text = !string.IsNullOrEmpty(user.Bio.StatusText) ? user.Bio.StatusText : user.Bio.Status.ToString();
            if(DescriptionText != null)
                DescriptionText.text = user.Bio.Description;
            if(StatusIcon != null)
            {
                switch (user.Bio.Status)
                {
                    case Status.Online:
                        StatusIcon.color = ColorTools.RGBtoHSV(44, 224, 44);
                        break;
                    case Status.Absent:
                        StatusIcon.color = ColorTools.RGBtoHSV(255, 187, 15);
                        break;
                    case Status.Party:
                        StatusIcon.color = ColorTools.RGBtoHSV(41, 185, 255);
                        break;
                    case Status.DoNotDisturb:
                        StatusIcon.color = ColorTools.RGBtoHSV(224, 44, 44);
                        break;
                    default:
                        StatusIcon.color = ColorTools.RGBtoHSV(128, 128, 128);
                        break;
                }
            }
            if(PronounRender != null)
                PronounRender.Render(user.Bio.Pronouns);
            if(ProfileIcon != null)
                ProfileIcon.RenderNetImage(user.Bio.PfpURL, Defaults.Instance.DefaultProfilePicture);
            if(ProfileBanner != null)
                ProfileBanner.RenderNetImage(user.Bio.BannerURL, Defaults.Instance.DefaultProfileBanner);
        }
    }
}