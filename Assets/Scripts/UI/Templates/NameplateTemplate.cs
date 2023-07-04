using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class NameplateTemplate : MonoBehaviour
    {
        public RawImage Banner;
        public RawImage Pfp;
        public Image Status;
        public TMP_Text Username;
        public GameObject PronounContainer;

        public Texture2D DefaultPfp;
        public Texture2D DefaultBanner;

        public Camera MainCamera;
        
        private TMP_Text pronounText;

        public void Render(User user)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.gameObject.SetActive(true);
            }
            Username.text = !string.IsNullOrEmpty(user.Bio.DisplayName) ? user.Bio.DisplayName : user.Username;
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
            if (!string.IsNullOrEmpty(user.Bio.PfpURL))
                DownloadTools.DownloadBytes(user.Bio.PfpURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = Pfp.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            Pfp.texture = ImageTools.BytesToTexture2D(user.Bio.PfpURL, bytes);
                    });
            else
                Pfp.texture = DefaultPfp;
            if (!string.IsNullOrEmpty(user.Bio.BannerURL))
                DownloadTools.DownloadBytes(user.Bio.BannerURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            Banner.texture = ImageTools.BytesToTexture2D(user.Bio.BannerURL, bytes);
                    });
            else
                Banner.texture = DefaultBanner;
        }

        private void Update()
        {
            transform.rotation =
                Quaternion.LookRotation((transform.position - MainCamera.transform.position).normalized);
        }
    }
}