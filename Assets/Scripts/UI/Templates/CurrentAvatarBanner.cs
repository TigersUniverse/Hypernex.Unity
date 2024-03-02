using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class CurrentAvatarBanner : MonoBehaviour
    {
        public static CurrentAvatarBanner Instance;
        
        public GameObject CurrentAvatarBannerObject;
        public CurrentAvatar CurrentAvatarPage;
        public TMP_Text AvatarName;
        public RawImage Banner;

        public Texture2D DefaultBanner;

        private AvatarCreator avatarCreator;

        public void OnNavigate() => CurrentAvatarPage.Render(avatarCreator);

        public void Render(AvatarCreator AvatarCreator, byte[] banner)
        {
            avatarCreator = AvatarCreator;
            AvatarName.text = LocalPlayer.Instance.avatarMeta.Name;
            if (banner.Length > 0)
                if (GifRenderer.IsGif(banner))
                {
                    GifRenderer gifRenderer = Banner.GetComponent<GifRenderer>();
                    if (gifRenderer != null)
                    {
                        Destroy(gifRenderer);
                        gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                    }
                    gifRenderer.LoadGif(banner);
                }
                else
                    Banner.texture = ImageTools.BytesToTexture2D(LocalPlayer.Instance.avatarMeta.ImageURL, banner);
            else
                Banner.texture = DefaultBanner;
            CurrentAvatarBannerObject.SetActive(true);
        }
        
        private void Start() => Instance = this;
        
        private void Update()
        {
            if(LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                CurrentAvatarBannerObject.SetActive(false);
        }
    }
}