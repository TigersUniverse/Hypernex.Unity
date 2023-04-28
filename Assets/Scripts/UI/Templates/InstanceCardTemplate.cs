using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.UIActions;
using Hypernex.Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class InstanceCardTemplate : MonoBehaviour
    {
        public TMP_Text WorldText;
        public TMP_Text CreatorText;
        public RawImage BannerImage;

        public Texture2D DefaultBanner;

        public Button NavigateButton;

        private LoginPageManager loginPageManager;
        private SafeInstance lastRenderedSafeInstance;
        private WorldMeta lastWorldMeta;
        private User lastHoster;

        public void Render(LoginPageManager lpm, SafeInstance instance, WorldMeta worldMeta, User hoster)
        {
            WorldText.text = worldMeta.Name;
            CreatorText.text = "Hosted By " + hoster.Username + " (" + instance.InstancePublicity + ")";
            if (!string.IsNullOrEmpty(worldMeta.ThumbnailURL))
                DownloadTools.DownloadBytes(worldMeta.ThumbnailURL,
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
            loginPageManager = lpm;
            lastRenderedSafeInstance = instance;
            lastWorldMeta = worldMeta;
            lastHoster = hoster;
        }
    
        private void Start() => NavigateButton.onClick.AddListener(() =>
        {
            // TODO: Make and Display Instance Page
            //if (lastRenderedSafeInstance != null)
            //loginPageManager.ProfileTemplate.Render(lastRenderedSafeInstance);
        });
    }
}