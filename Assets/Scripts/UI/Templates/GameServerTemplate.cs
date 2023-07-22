using System;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Templates
{
    public class GameServerTemplate : MonoBehaviour
    {
        public CreateInstanceTemplate CreateInstanceTemplate;
        public TMP_Text ContinentCode;
        public TMP_Text CityStateCountry;
        public TMP_Text GameServerIdText;

        private GameServer lastGameServer;

        public void Render(GameServer gameServer)
        {
            ContinentCode.text = gameServer.Region.ContinentCode;
            if (!string.IsNullOrEmpty(gameServer.Region.Country) && !string.IsNullOrEmpty(gameServer.Region.State) &&
                !string.IsNullOrEmpty(gameServer.Region.City))
                CityStateCountry.text = $"{gameServer.Region.City}, {gameServer.Region.State}\n" +
                                        $"<size=18>{gameServer.Region.Country}</size>";
            else if(!string.IsNullOrEmpty(gameServer.Region.State) && !string.IsNullOrEmpty(gameServer.Region.City))
                CityStateCountry.text = $"{gameServer.Region.City}, {gameServer.Region.State}";
            else if(!string.IsNullOrEmpty(gameServer.Region.Country))
                CityStateCountry.text = $"{gameServer.Region.Country}";
            else
                CityStateCountry.text = String.Empty;
            GameServerIdText.text = gameServer.GameServerId;
            lastGameServer = gameServer;
        }

        public void OnSelect() => CreateInstanceTemplate.SetGameServer(lastGameServer);
    }
}