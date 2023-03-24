using Hypernex.Player;
using Hypernex.UI;
using UnityEngine;

public class Init : MonoBehaviour
{
    public UITheme DefaultTheme;

    void Start()
    {
        DefaultTheme.ApplyThemeToUI();
    }

    private void OnApplicationQuit()
    {
        if (APIPlayer.UserSocket != null && APIPlayer.UserSocket.IsOpen)
            APIPlayer.UserSocket.Close();
    }
}
