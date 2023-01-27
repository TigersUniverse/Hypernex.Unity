using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Init : MonoBehaviour
{
    public UITheme DefaultTheme;
    
    // Start is called before the first frame update
    void Start()
    {
        DefaultTheme.ApplyThemeToUI();
    }
}
