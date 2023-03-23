using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.UIActions;
using Hypernex.Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class ProfileTemplate : MonoBehaviour
    {
        public LoginPageTopBarButton ProfilePage;
    
        public RawImage Banner;
        public RawImage Pfp;
        public Image Status;
        public GameObject PronounContainer;
        public TMP_Text Username;
        public TMP_Text StatusText;
        public TMP_Text DescriptionText;

        public Texture2D DefaultPfp;
        public Texture2D DefaultBanner;

        private TMP_Text pronounText;
    
        public void Render(User user, bool skipShow = false)
        {
            if (!string.IsNullOrEmpty(user.Bio.DisplayName))
                Username.text = user.Bio.DisplayName + " <size=15>@" + user.Username + "</size>";
            else
                Username.text = "@" + user.Username;
            StatusText.text = !string.IsNullOrEmpty(user.Bio.StatusText) ? user.Bio.StatusText : user.Bio.Status.ToString();
            DescriptionText.text = user.Bio.Description;
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
            if (user.Bio.Pronouns != null)
            {
                (pronounText == null ? pronounText = PronounContainer.transform.GetChild(0).GetComponent<TMP_Text>() : pronounText)!.text =
                    user.Bio.Pronouns.ToString();
                PronounContainer.SetActive(true);
            }
            else
                PronounContainer.SetActive(false);
            if(ComponentTools.HasComponent<GifRenderer>(Pfp.gameObject))
                Destroy(Pfp.gameObject.GetComponent<GifRenderer>());
            if(ComponentTools.HasComponent<GifRenderer>(Banner.gameObject))
                Destroy(Banner.gameObject.GetComponent<GifRenderer>());
            if (!string.IsNullOrEmpty(user.Bio.PfpURL))
                DownloadTools.DownloadBytes(user.Bio.PfpURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = Pfp.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            Pfp.texture = ImageTools.BytesToTexture2D(bytes);
                    });
            else
                Pfp.texture = DefaultPfp;
            if (!string.IsNullOrEmpty(user.Bio.BannerURL))
                DownloadTools.DownloadBytes(user.Bio.BannerURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = Banner.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            Banner.texture = ImageTools.BytesToTexture2D(bytes);
                    });
            else
                Banner.texture = DefaultBanner;
            if(!skipShow)
                ProfilePage.Show();
        }
    }
}