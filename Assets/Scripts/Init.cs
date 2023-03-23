using Hypernex.UI;
using UnityEngine;

public class Init : MonoBehaviour
{
    public UITheme DefaultTheme;

    void Start()
    {
        DefaultTheme.ApplyThemeToUI();
    }
}
