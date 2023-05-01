using Hypernex.Player;
using HypernexSharp.APIObjects;
using HypernexSharp.SocketObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Templates
{
    public class CreateInstanceTemplate : MonoBehaviour
    {
        public TMP_Text WorldName;
        public TMP_Text WorldCreator;
        public TMP_Text InstancePublicityLabel;
        public TMP_Text InstanceProtocolLabel;

        private InstancePublicity instancePublicity = InstancePublicity.Anyone;
        private InstanceProtocol instanceProtocol = InstanceProtocol.KCP;
        private WorldMeta worldMeta;

        public void Render(WorldMeta WorldMeta, User creator)
        {
            WorldName.text = WorldMeta.Name;
            WorldCreator.text = "World By " + creator.Username;
            worldMeta = WorldMeta;
            gameObject.SetActive(true);
        }

        public void Anyone() => instancePublicity = InstancePublicity.Anyone;
        public void Acquaintances() => instancePublicity = InstancePublicity.Acquaintances;
        public void Friends() => instancePublicity = InstancePublicity.Friends;
        public void OpenRequest() => instancePublicity = InstancePublicity.OpenRequest;
        public void ModeratorRequest() => instancePublicity = InstancePublicity.ModeratorRequest;
        public void ClosedRequest() => instancePublicity = InstancePublicity.ClosedRequest;

        public void KCP() => instanceProtocol = InstanceProtocol.KCP;
        public void TCP() => instanceProtocol = InstanceProtocol.TCP;
        public void UDP() => instanceProtocol = InstanceProtocol.UDP;

        public void Create()
        {
            SocketManager.CreateInstance(worldMeta, instancePublicity, instanceProtocol);
            Return();
        }
        public void Return() => gameObject.SetActive(false);

        public void Update()
        {
            InstancePublicityLabel.text = "Instance Publicity: " + instancePublicity;
            InstanceProtocolLabel.text = "Instance Protocol: " + instanceProtocol;
        }
    }
}