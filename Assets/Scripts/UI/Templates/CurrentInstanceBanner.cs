using Hypernex.Game;
using Hypernex.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class CurrentInstanceBanner : MonoBehaviour
    {
        public static CurrentInstanceBanner Instance;
        
        public GameObject CurrentInstanceBannerObject;
        public CurrentInstance CurrentInstancePage;
        public TMP_Text WorldName;
        public RawImage Banner;

        public Texture2D DefaultBanner;

        private GameInstance g;
        private byte[] b;

        public void OnNavigate() => CurrentInstancePage.Render(g, b, g.ConnectedUsers);

        public void Render(GameInstance gameInstance, byte[] banner)
        {
            g = gameInstance;
            b = banner;
            WorldName.text = gameInstance.worldMeta.Name;
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
                    Banner.texture = ImageTools.BytesToTexture2D(gameInstance.worldMeta.ThumbnailURL, banner);
            else
                Banner.texture = DefaultBanner;
            CurrentInstanceBannerObject.SetActive(true);
        }

        private void Start() => Instance = this;

        private void Update()
        {
            if(GameInstance.FocusedInstance == null)
                CurrentInstanceBannerObject.SetActive(false);
        }
    }
}