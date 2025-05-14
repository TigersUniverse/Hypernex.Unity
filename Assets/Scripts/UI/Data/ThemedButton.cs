using Hypernex.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UIActions.Data
{
    public struct ThemedButton
    {
        public Button Button;
        public UIThemeObject UIThemeObject;

        public ThemedButton(GameObject g)
        {
            Button = g.GetComponent<Button>();
            UIThemeObject = g.GetComponent<UIThemeObject>();
        }
    }
}