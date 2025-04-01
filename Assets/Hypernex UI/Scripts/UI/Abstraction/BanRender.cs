using System.Globalization;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;

namespace Hypernex.UI.Abstraction
{
    public class BanRender : UIRender, IRender<BanStatus>
    {
        public TMP_Text Header;
        public TMP_Text Reasoning;
        
        public void Render(BanStatus t)
        {
            if (Header != null)
                Header.text = "You have been banned!";
            if (Reasoning != null)
            {
                string banBegin = DateTools.UnixTimeStampToDateTime(t.BanBegin)
                    .ToString(CultureInfo.InvariantCulture);
                string banEnd = DateTools.UnixTimeStampToDateTime(t.BanEnd)
                    .ToString(CultureInfo.InvariantCulture);
                Reasoning.text = $"Banned On: {banBegin}\n" +
                                 $"Ban Ended: {banEnd}\n" +
                                 $"Reason: {t.BanReason}\n" +
                                 $"Description: {t.BanDescription}";
            }
        }
    }
}