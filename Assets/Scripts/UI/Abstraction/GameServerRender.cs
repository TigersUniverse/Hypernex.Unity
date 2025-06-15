using System;
using HypernexSharp.APIObjects;
using TMPro;

namespace Hypernex.UI.Abstraction
{
    public class GameServerRender : UIRender, IRender<GameServer>
    {
        public TMP_Text ContinentCode;
        public TMP_Text CityStateCountry;
        
        internal GameServer gameServer;
        
        public void Render(GameServer t)
        {
            gameServer = t;
            ContinentCode.text = gameServer.Region.ContinentCode;
            if (!string.IsNullOrEmpty(gameServer.Region.Country) && !string.IsNullOrEmpty(gameServer.Region.State) &&
                !string.IsNullOrEmpty(gameServer.Region.City))
                CityStateCountry.text = $"{gameServer.Region.City}, {gameServer.Region.State}\n" +
                                        $"{gameServer.Region.Country}";
            else if(!string.IsNullOrEmpty(gameServer.Region.State) && !string.IsNullOrEmpty(gameServer.Region.City))
                CityStateCountry.text = $"{gameServer.Region.City}, {gameServer.Region.State}";
            else if(!string.IsNullOrEmpty(gameServer.Region.Country))
                CityStateCountry.text = $"{gameServer.Region.Country}";
            else
                CityStateCountry.text = String.Empty;
        }
    }
}