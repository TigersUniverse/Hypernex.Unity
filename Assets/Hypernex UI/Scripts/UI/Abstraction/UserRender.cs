using System;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction
{
    public class UserRender : UIRender, IRender<User>
    {
        public RawImage ProfileIcon;
        public RawImage ProfileBanner;
        public Image StatusIcon;
        [Tooltip("Can be null. If null, this assumes the Username field and will show the display.")]
        public TMP_Text DisplayName;
        public TMP_Text Username;
        public TMP_Text StatusText;
        public TMP_Text DescriptionText;
        public PronounRender PronounRender;

        internal User u;

        public void Render(User user)
        {
            if (DisplayName != null && !string.IsNullOrEmpty(user.Bio.DisplayName))
            {
                DisplayName.text = user.Bio.DisplayName;
                Username.text = "@" + user.Username;
            }
            else if (DisplayName != null)
            {
                DisplayName.text = user.GetUserDisplayName();
                Username.text = String.Empty;
            }
            else if (Username != null)
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
            u = user;
        }
    }
}