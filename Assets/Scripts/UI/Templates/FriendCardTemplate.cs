using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.UIActions;
using Hypernex.Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class FriendCardTemplate : MonoBehaviour
    {
        public TMP_Text UsernameText;
        public TMP_Text StatusText;
        public RawImage PfpImage;
        public Image Status;
        public RawImage BannerImage;

        public Texture2D DefaultPfp;
        public Texture2D DefaultBanner;

        public Button NavigateButton;

        private LoginPageManager loginPageManager;
        private User lastRenderedUser;

        public void Render(LoginPageManager instance, User user)
        {
            if (!string.IsNullOrEmpty(user.Bio.DisplayName))
                UsernameText.text = user.Bio.DisplayName + " <size=15>@" + user.Username + "</size>";
            else
                UsernameText.text = "@" + user.Username;
            StatusText.text = !string.IsNullOrEmpty(user.Bio.StatusText) ? user.Bio.StatusText : user.Bio.Status.ToString();
            switch (user.Bio.Status)
            {
                case HypernexSharp.APIObjects.Status.Online:
                    Status.color = ColorTools.RGBtoHSV(44, 224, 44);
                    break;
                case HypernexSharp.APIObjects.Status.Absent:
                    Status.color = ColorTools.RGBtoHSV(255, 187, 15);
                    break;
                case HypernexSharp.APIObjects.Status.Party:
                    Status.color = ColorTools.RGBtoHSV(41, 185, 255);
                    break;
                case HypernexSharp.APIObjects.Status.DoNotDisturb:
                    Status.color = ColorTools.RGBtoHSV(224, 44, 44);
                    break;
                default:
                    Status.color = ColorTools.RGBtoHSV(128, 128, 128);
                    break;
            }
            if (!string.IsNullOrEmpty(user.Bio.PfpURL))
                DownloadTools.DownloadBytes(user.Bio.PfpURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = PfpImage.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            PfpImage.texture = ImageTools.BytesToTexture2D(bytes);
                    });
            else
                PfpImage.texture = DefaultPfp;
            if (!string.IsNullOrEmpty(user.Bio.BannerURL))
                DownloadTools.DownloadBytes(user.Bio.BannerURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = BannerImage.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            BannerImage.texture = ImageTools.BytesToTexture2D(bytes);
                    });
            else
                BannerImage.texture = DefaultBanner;
            loginPageManager = instance;
            lastRenderedUser = user;
        }

        private void Start() => NavigateButton.onClick.AddListener(() =>
        {
            if (lastRenderedUser != null)
                loginPageManager.ProfileTemplate.Render(lastRenderedUser);
        });
    }
}
