using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class UserInstanceCardTemplate : MonoBehaviour
    {
        public ProfileTemplate ProfileTemplate;
        public RawImage Pfp;
        public TMP_Text Username;

        public Texture2D DefaultPfp;

        private User User;
        private GameObject Overlay;

        public void OnNavigate()
        {
            if(Overlay != null)
                Overlay.SetActive(false);
            ProfileTemplate.Render(User);
        }

        public void Render(User user, GameObject overlay = null)
        {
            Username.text = user.Username;
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
            User = user;
            Overlay = overlay;
        }
    }
}