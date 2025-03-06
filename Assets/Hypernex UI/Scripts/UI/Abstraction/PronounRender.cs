using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;

namespace Hypernex.UI.Abstraction
{
    public class PronounRender : UIRender, IRender<Pronouns>
    {
        public GameObject PronounContainer;
        public TMP_Text PronounText;
        
        public void Render(Pronouns pronouns)
        {
            if (pronouns != null && PronounText != null)
            {
                (PronounText == null
                    ? PronounText = PronounContainer.transform.GetChild(0).GetComponent<TMP_Text>()
                    : PronounText).text = pronouns.ToString();
                PronounContainer.SetActive(true);
            }
            else
                PronounContainer.SetActive(false);
        }
    }
}