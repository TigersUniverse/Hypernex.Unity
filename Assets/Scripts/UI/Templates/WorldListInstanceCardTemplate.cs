using HypernexSharp.APIObjects;
using TMPro;
using Hypernex.UIActions;
using Hypernex.Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class WorldListInstanceCardTemplate : MonoBehaviour
    {
        public TMP_Text CardText;

        public Button NavigateButton;

        private SafeInstance lastRenderedSafeInstance;
        private WorldMeta lastWorldMeta;
        private User lastHoster;

        public void Render(SafeInstance instance, WorldMeta worldMeta, User host)
        {
            CardText.richText = true;
            CardText.text = $"{instance.InstanceId}\n" +
                            $"{host.Username}\n" +
                            $"{instance.InstancePublicity}";
        }
    
        private void Start() => NavigateButton.onClick.AddListener(() =>
        {
            // TODO: Instance Page
            //if (lastRenderedSafeInstance != null)
            //loginPageManager.ProfileTemplate.Render(lastRenderedSafeInstance);
        });
    }
}