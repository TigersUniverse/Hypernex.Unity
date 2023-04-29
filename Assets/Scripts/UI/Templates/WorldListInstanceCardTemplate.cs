using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class WorldListInstanceCardTemplate : MonoBehaviour
    {
        public JoinInstanceTemplate JoinInstanceTemplate;
        
        public TMP_Text CardText;

        public Button NavigateButton;

        private SafeInstance lastRenderedSafeInstance;
        private WorldMeta lastWorldMeta;
        private User lastHoster, lastCreator;

        public void Render(SafeInstance instance, WorldMeta worldMeta, User host, User creator)
        {
            CardText.richText = true;
            CardText.text = $"{instance.InstanceId}\n" +
                            $"{host.Username}\n" +
                            $"{instance.InstancePublicity}";
            lastRenderedSafeInstance = instance;
            lastWorldMeta = worldMeta;
            lastHoster = host;
            lastCreator = creator;
        }
    
        private void Start() => NavigateButton.onClick.AddListener(() =>
        {
            JoinInstanceTemplate.Render(lastRenderedSafeInstance, lastWorldMeta, lastHoster, lastCreator);
            JoinInstanceTemplate.gameObject.SetActive(true);
        });
    }
}