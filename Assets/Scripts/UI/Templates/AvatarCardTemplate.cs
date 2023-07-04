using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.UIActions;
using Hypernex.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class AvatarCardTemplate : MonoBehaviour
    {
        public TMP_Text AvatarText;
        public TMP_Text CreatorText;
        public RawImage BannerImage;
        public Texture2D DefaultBanner;
        public Button NavigateButton;

        private AvatarTemplate avatarTemplate;
        private AvatarMeta lastAvatarMeta;
        private User lastCreator;
        private LoginPageTopBarButton PreviousPage;

        public void Render(AvatarTemplate at, LoginPageTopBarButton pp, AvatarMeta avatarMeta, User creator)
        {
            AvatarText.text = avatarMeta.Name;
            CreatorText.text = "Created By " + creator.Username;
            if (!string.IsNullOrEmpty(avatarMeta.ImageURL))
                DownloadTools.DownloadBytes(avatarMeta.ImageURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = BannerImage.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            BannerImage.texture = ImageTools.BytesToTexture2D(avatarMeta.ImageURL, bytes);
                    });
            else
                BannerImage.texture = DefaultBanner;
            avatarTemplate = at;
            PreviousPage = pp;
            lastAvatarMeta = avatarMeta;
            lastCreator = creator;
        }

        private void Start() =>
            NavigateButton.onClick.AddListener(() => avatarTemplate.Render(lastAvatarMeta, lastCreator, PreviousPage));
    }
}