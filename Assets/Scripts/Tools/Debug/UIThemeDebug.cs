using Hypernex.UI;
using UnityEngine;

namespace Hypernex.Tools.Debug
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class UIThemeDebug : MonoBehaviour
    {
        public UITheme ThemeToApply;

        public void Start()
        {
            if(ThemeToApply == null) return;
            ThemeToApply.ApplyThemeToUI(true);
        }
    }
}