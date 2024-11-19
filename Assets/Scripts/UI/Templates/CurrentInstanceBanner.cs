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

        private GameInstance g;
        private byte[] b;

        public void OnNavigate() => CurrentInstancePage.Render(g, b, g.ConnectedUsers);

        public void Render(GameInstance gameInstance, byte[] banner)
        {
            g = gameInstance;
            b = banner;
            WorldName.text = gameInstance.worldMeta.Name;
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