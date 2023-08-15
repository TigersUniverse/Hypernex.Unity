using System.Collections.Generic;
using Hypernex.Game;
using Hypernex.Tools;
using Hypernex.UIActions;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class CurrentInstance : MonoBehaviour
    {
        public LoginPageTopBarButton Page;
        public TMP_Text WorldName;
        public RawImage Banner;
        public TMP_Text WorldCreator;
        public TMP_Text DescriptionText;
        public DynamicScroll Users;

        public Texture2D DefaultBanner;

        private GameInstance g;
        private LoginPageTopBarButton lastPage;
        
        private void CreateCurrentInstanceUserCard(User user)
        {
            GameObject instanceCard = DontDestroyMe.GetNotDestroyedObject("UITemplates").transform
                .Find("CurrentInstanceUserCard").gameObject;
            GameObject newInstanceCard = Instantiate(instanceCard);
            RectTransform c = newInstanceCard.GetComponent<RectTransform>();
            newInstanceCard.GetComponent<UserInstanceCardTemplate>().Render(user);
            Users.AddItem(c);
        }

        public void Render(GameInstance gameInstance, byte[] banner, List<User> connectedUsers)
        {
            g = gameInstance;
            WorldName.text = gameInstance.worldMeta.Name;
            WorldCreator.text = string.IsNullOrEmpty(gameInstance.host.Bio.DisplayName)
                ? gameInstance.host.Username
                : $"{gameInstance.host.Bio.DisplayName} <size=15>(@{gameInstance.host.Username})</size>";
            DescriptionText.text = gameInstance.worldMeta.Description;
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
            Users.Clear();
            Page.Show();
            foreach (User connectedUser in connectedUsers)
                CreateCurrentInstanceUserCard(connectedUser);
        }

        public void LeaveButtonClick()
        {
            if(GameInstance.FocusedInstance != null)
                GameInstance.FocusedInstance.Dispose();
            LoginPageTopBarButton.Show("Home");
        }
    }
}